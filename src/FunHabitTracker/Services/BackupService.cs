using System.Text.Json;
using System.Text.Json.Serialization;
using FunHabitTracker.Data;
using FunHabitTracker.Infrastructure;
using FunHabitTracker.Models;
using FunHabitTracker.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FunHabitTracker.Services;

public sealed class BackupService
{
    public const int FormatVersion = 1;
    private static readonly SemaphoreSlim DatabaseMutation = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly AppDbContext _context;
    private readonly IAppPaths _paths;
    private readonly IClock _clock;
    private readonly long _maxImportBytes;

    public BackupService(AppDbContext context, IAppPaths paths, IClock clock, IConfiguration configuration)
    {
        _context = context;
        _paths = paths;
        _clock = clock;
        _maxImportBytes = configuration.GetValue<long?>("FunHabitTracker:MaxImportBytes") ?? 5 * 1024 * 1024;
    }

    public async Task WriteJsonExportAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        var document = new BackupDocument(
            FormatVersion,
            _clock.UtcNow,
            "FunHabitTracker",
            await _context.Habits.AsNoTracking().OrderBy(item => item.Id).Select(item => new HabitBackupItem(item.Id, item.Name, item.Description, item.StartDate, item.RecurrenceType, item.ScheduledDays, item.IsArchived, item.CreatedAt, item.UpdatedAt, item.ArchivedAt)).ToArrayAsync(cancellationToken),
            await _context.HabitCompletions.AsNoTracking().OrderBy(item => item.HabitId).ThenBy(item => item.Date).Select(item => new OutcomeBackupItem(item.Id, item.HabitId, item.Date, item.Outcome)).ToArrayAsync(cancellationToken));
        await JsonSerializer.SerializeAsync(destination, document, JsonOptions, cancellationToken);
    }

    public async Task<ImportPreviewModel> StageJsonImportAsync(Stream source, long length, CancellationToken cancellationToken = default)
    {
        if (length <= 0 || length > _maxImportBytes) throw new InvalidDataException($"Die Importdatei muss zwischen 1 Byte und {_maxImportBytes / 1024 / 1024} MB gross sein.");
        _paths.EnsureCreated();
        var token = Guid.NewGuid().ToString("N");
        var stagedPath = GetStagedPath(token, requireExisting: false);
        await using (var target = new FileStream(stagedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await source.CopyToAsync(target, cancellationToken);
            if (target.Length > _maxImportBytes) { target.Close(); File.Delete(stagedPath); throw new InvalidDataException("Die Importdatei ist zu gross."); }
        }
        try
        {
            var document = await ReadAndValidateAsync(stagedPath, cancellationToken);
            return new ImportPreviewModel(token, document.Habits.Length, document.Outcomes.Length, document.ExportedAtUtc);
        }
        catch
        {
            File.Delete(stagedPath);
            throw;
        }
    }

    public async Task ImportJsonAsync(string token, CancellationToken cancellationToken = default)
    {
        var stagedPath = GetStagedPath(token);
        var document = await ReadAndValidateAsync(stagedPath, cancellationToken);
        await DatabaseMutation.WaitAsync(cancellationToken);
        try
        {
            await CreateDatabaseBackupAsync("pre-import", cancellationToken);
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            _context.HabitCompletions.RemoveRange(_context.HabitCompletions);
            _context.Habits.RemoveRange(_context.Habits);
            await _context.SaveChangesAsync(cancellationToken);

            _context.Habits.AddRange(document.Habits.Select(item => new Habit
            {
                Id = item.Id,
                Name = item.Name.Trim(),
                Description = item.Description,
                StartDate = item.StartDate,
                RecurrenceType = item.RecurrenceType,
                ScheduledDays = item.RecurrenceType == RecurrenceType.Daily ? ScheduledDays.EveryDay : item.ScheduledDays,
                IsArchived = item.IsArchived,
                IsActive = !item.IsArchived,
                Frequency = item.RecurrenceType == RecurrenceType.Daily ? "Daily" : "SelectedWeekdays",
                CreatedAt = item.CreatedAtUtc,
                UpdatedAt = item.UpdatedAtUtc,
                ArchivedAt = item.ArchivedAtUtc
            }));
            await _context.SaveChangesAsync(cancellationToken);
            _context.HabitCompletions.AddRange(document.Outcomes.Select(item => new HabitCompletion { Id = item.Id, HabitId = item.HabitId, Date = item.Date, Outcome = item.Outcome, IsDone = item.Outcome == HabitOutcome.Done }));
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            File.Delete(stagedPath);
        }
        finally
        {
            DatabaseMutation.Release();
        }
    }

    public async Task<string> CreateDatabaseBackupAsync(string reason, CancellationToken cancellationToken = default)
    {
        _paths.EnsureCreated();
        var destinationPath = Path.Combine(_paths.BackupsPath, $"habits-{reason}-{_clock.UtcNow:yyyyMMdd-HHmmssfff}.db");
        await _context.Database.CloseConnectionAsync();
        await using var source = new SqliteConnection($"Data Source={_paths.DatabasePath};Mode=ReadOnly;Pooling=False");
        await using var destination = new SqliteConnection($"Data Source={destinationPath};Pooling=False");
        await source.OpenAsync(cancellationToken);
        await destination.OpenAsync(cancellationToken);
        source.BackupDatabase(destination);
        await EnsureIntegrityAsync(destination, cancellationToken);
        return destinationPath;
    }

    public async Task RestoreDatabaseAsync(Stream source, long length, CancellationToken cancellationToken = default)
    {
        if (length <= 0 || length > 250 * 1024 * 1024) throw new InvalidDataException("Die SQLite-Datei ist leer oder groesser als 250 MB.");
        _paths.EnsureCreated();
        var candidate = Path.Combine(_paths.ImportsPath, $"database-{Guid.NewGuid():N}.db");
        await using (var target = new FileStream(candidate, FileMode.CreateNew, FileAccess.Write, FileShare.None)) await source.CopyToAsync(target, cancellationToken);

        await DatabaseMutation.WaitAsync(cancellationToken);
        try
        {
            await ValidateDatabaseCandidateAsync(candidate, cancellationToken);
            var previous = await CreateDatabaseBackupAsync("pre-restore", cancellationToken);
            await _context.Database.CloseConnectionAsync();
            SqliteConnection.ClearAllPools();
            File.Copy(candidate, _paths.DatabasePath, true);
            try
            {
                await using var restored = new SqliteConnection($"Data Source={_paths.DatabasePath};Mode=ReadOnly;Pooling=False");
                await restored.OpenAsync(cancellationToken);
                await EnsureIntegrityAsync(restored, cancellationToken);
            }
            catch
            {
                SqliteConnection.ClearAllPools();
                File.Copy(previous, _paths.DatabasePath, true);
                throw;
            }
        }
        finally
        {
            if (File.Exists(candidate)) File.Delete(candidate);
            DatabaseMutation.Release();
        }
    }

    private async Task<BackupDocument> ReadAndValidateAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        BackupDocument? document;
        try { document = await JsonSerializer.DeserializeAsync<BackupDocument>(stream, JsonOptions, cancellationToken); }
        catch (JsonException exception) { throw new InvalidDataException("Die Datei ist kein gueltiges FunHabitTracker-Backup.", exception); }
        if (document is null || document.FormatVersion != FormatVersion || document.Product != "FunHabitTracker") throw new InvalidDataException("Die Backup-Version wird nicht unterstuetzt.");
        if (document.Habits.Length > 10_000 || document.Outcomes.Length > 1_000_000) throw new InvalidDataException("Das Backup enthaelt zu viele Datensaetze.");
        if (document.Habits.Select(item => item.Id).Distinct().Count() != document.Habits.Length) throw new InvalidDataException("Habit-IDs sind nicht eindeutig.");
        var ids = document.Habits.Select(item => item.Id).ToHashSet();
        if (document.Habits.Any(item => item.Id <= 0 || string.IsNullOrWhiteSpace(item.Name) || item.Name.Trim().Length > 200 || item.Description?.Length > 1000 || !Enum.IsDefined(item.RecurrenceType) || (item.RecurrenceType == RecurrenceType.SelectedWeekdays && item.ScheduledDays == ScheduledDays.None))) throw new InvalidDataException("Mindestens ein Habit ist ungueltig.");
        if (document.Outcomes.Any(item => item.Id <= 0 || !ids.Contains(item.HabitId) || !Enum.IsDefined(item.Outcome))) throw new InvalidDataException("Mindestens ein Verlaufseintrag ist ungueltig.");
        if (document.Outcomes.Select(item => item.Id).Distinct().Count() != document.Outcomes.Length || document.Outcomes.Select(item => (item.HabitId, item.Date)).Distinct().Count() != document.Outcomes.Length) throw new InvalidDataException("Verlaufseintraege sind nicht eindeutig.");
        return document;
    }

    private async Task ValidateDatabaseCandidateAsync(string path, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        await connection.OpenAsync(cancellationToken);
        await EnsureIntegrityAsync(connection, cancellationToken);
        foreach (var table in new[] { "Habits", "HabitCompletions", "__EFMigrationsHistory" })
        {
            var command = connection.CreateCommand(); command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name"; command.Parameters.AddWithValue("$name", table);
            if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 0) throw new InvalidDataException("Die SQLite-Datei hat kein kompatibles FunHabitTracker-Schema.");
        }
        var migration = connection.CreateCommand(); migration.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId = '20260823120000_LocalFirstFoundation'";
        if (Convert.ToInt32(await migration.ExecuteScalarAsync(cancellationToken)) == 0) throw new InvalidDataException("Die SQLite-Datei verwendet eine nicht unterstuetzte Schema-Version.");
    }

    private static async Task EnsureIntegrityAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand(); command.CommandText = "PRAGMA integrity_check";
        if (!string.Equals(Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)), "ok", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Die SQLite-Integritaetspruefung ist fehlgeschlagen.");
    }

    private string GetStagedPath(string token, bool requireExisting = true)
    {
        if (token.Length != 32 || token.Any(character => !Uri.IsHexDigit(character))) throw new InvalidDataException("Der Importvorgang ist ungueltig.");
        var path = Path.Combine(_paths.ImportsPath, $"json-{token}.json");
        if (requireExisting && !File.Exists(path)) throw new FileNotFoundException("Der vorbereitete Import ist nicht mehr vorhanden.");
        return path;
    }

    private sealed record BackupDocument(int FormatVersion, DateTime ExportedAtUtc, string Product, HabitBackupItem[] Habits, OutcomeBackupItem[] Outcomes);
    private sealed record HabitBackupItem(int Id, string Name, string? Description, DateOnly StartDate, RecurrenceType RecurrenceType, ScheduledDays ScheduledDays, bool IsArchived, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, DateTime? ArchivedAtUtc);
    private sealed record OutcomeBackupItem(int Id, int HabitId, DateOnly Date, HabitOutcome Outcome);
}

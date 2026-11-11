using FunHabitTracker.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FunHabitTracker.Infrastructure;

public sealed class DatabaseInitializer
{
    internal const string InitialMigrationId = "20251115194945_AddHabitCompletionTable";
    private readonly AppDbContext _db;
    private readonly IAppPaths _paths;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IHostEnvironment _environment;
    private readonly bool _importLegacyDatabaseLocation;

    public DatabaseInitializer(
        AppDbContext db,
        IAppPaths paths,
        ILogger<DatabaseInitializer> logger,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _db = db;
        _paths = paths;
        _logger = logger;
        _environment = environment;
        var defaultRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FunHabitTracker");
        _importLegacyDatabaseLocation = configuration.GetValue<bool?>(
            "FunHabitTracker:ImportLegacyDatabaseLocation")
            ?? string.Equals(paths.Root, defaultRoot, StringComparison.OrdinalIgnoreCase);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _paths.EnsureCreated();
        await ImportLegacyDatabaseLocationAsync(cancellationToken);
        await AdoptLegacyEnsureCreatedDatabaseAsync(cancellationToken);

        var pending = (await _db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        if (pending.Length > 0 && File.Exists(_paths.DatabasePath))
        {
            await CreateSafetyBackupAsync("pre-migration", cancellationToken);
        }

        _logger.LogInformation("Applying {Count} pending database migration(s) to {DatabasePath}", pending.Length, _paths.DatabasePath);
        await _db.Database.MigrateAsync(cancellationToken);
    }

    private async Task ImportLegacyDatabaseLocationAsync(CancellationToken cancellationToken)
    {
        if (!_importLegacyDatabaseLocation)
        {
            return;
        }

        var targetHabitCount = await TryGetHabitCountAsync(_paths.DatabasePath, cancellationToken);
        if (targetHabitCount != 0)
        {
            return;
        }

        var targetPath = Path.GetFullPath(_paths.DatabasePath);
        var candidates = new[]
        {
            Path.Combine(_environment.ContentRootPath, "habits.db"),
            Path.Combine(Directory.GetCurrentDirectory(), "habits.db")
        };

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var candidatePath = Path.GetFullPath(candidate);
            if (string.Equals(candidatePath, targetPath, StringComparison.OrdinalIgnoreCase)
                || await TryGetHabitCountAsync(candidatePath, cancellationToken) is not > 0)
            {
                continue;
            }

            if (File.Exists(targetPath) && new FileInfo(targetPath).Length > 0)
            {
                await CreateSafetyBackupAsync("pre-location-import", cancellationToken);
            }

            var temporaryPath = $"{targetPath}.import-{Guid.NewGuid():N}.tmp";
            try
            {
                await using var source = new SqliteConnection($"Data Source={candidatePath};Mode=ReadOnly;Pooling=False");
                await using var target = new SqliteConnection($"Data Source={temporaryPath};Pooling=False");
                await source.OpenAsync(cancellationToken);
                await target.OpenAsync(cancellationToken);
                source.BackupDatabase(target);
                await target.CloseAsync();
                await source.CloseAsync();
                SqliteConnection.ClearAllPools();
                File.Move(temporaryPath, targetPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

            _logger.LogInformation(
                "Imported legacy database from {LegacyPath} to {DatabasePath}",
                candidatePath,
                targetPath);
            return;
        }
    }

    private async Task AdoptLegacyEnsureCreatedDatabaseAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_paths.DatabasePath) || new FileInfo(_paths.DatabasePath).Length == 0)
        {
            return;
        }

        await using var connection = new SqliteConnection($"Data Source={_paths.DatabasePath}");
        await connection.OpenAsync(cancellationToken);
        var hasHabits = await TableExistsAsync(connection, "Habits", cancellationToken);
        var hasHistory = await TableExistsAsync(connection, "__EFMigrationsHistory", cancellationToken);
        if (!hasHabits || hasHistory)
        {
            return;
        }

        if (!await TableExistsAsync(connection, "HabitCompletions", cancellationToken))
        {
            throw new InvalidOperationException("Legacy database is missing the HabitCompletions table and cannot be adopted automatically.");
        }

        await CreateSafetyBackupAsync("legacy-adoption", cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ($migration, '8.0.4');
            """;
        command.Parameters.AddWithValue("$migration", InitialMigrationId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Adopted legacy EnsureCreated database at {DatabasePath}", _paths.DatabasePath);
    }

    private async Task CreateSafetyBackupAsync(string reason, CancellationToken cancellationToken)
    {
        if (!File.Exists(_paths.DatabasePath))
        {
            return;
        }

        _paths.EnsureCreated();
        var destination = Path.Combine(
            _paths.BackupsPath,
            $"habits-{reason}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.db");
        await using var source = new FileStream(_paths.DatabasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        await using var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await source.CopyToAsync(target, cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(
        SqliteConnection connection,
        string table,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name";
        command.Parameters.AddWithValue("$name", table);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private static async Task<int?> TryGetHabitCountAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path) || new FileInfo(path).Length == 0)
        {
            return 0;
        }

        try
        {
            await using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
            await connection.OpenAsync(cancellationToken);
            if (!await TableExistsAsync(connection, "Habits", cancellationToken))
            {
                return null;
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Habits";
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        }
        catch (SqliteException)
        {
            return null;
        }
    }
}

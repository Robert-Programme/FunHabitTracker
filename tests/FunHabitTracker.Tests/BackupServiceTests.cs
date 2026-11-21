using System.Text;
using FunHabitTracker.Data;
using FunHabitTracker.Infrastructure;
using FunHabitTracker.Models;
using FunHabitTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunHabitTracker.Tests;

[TestClass]
public sealed class BackupServiceTests
{
    [TestMethod]
    public async Task JsonExportImport_RoundTripsAllUserData()
    {
        await using var fixture = await BackupFixture.CreateAsync();
        var habit = new Habit { Name = "Read", Description = "20 pages", StartDate = new DateOnly(2026, 8, 1), IsActive = true };
        fixture.Context.Habits.Add(habit); await fixture.Context.SaveChangesAsync();
        fixture.Context.HabitCompletions.Add(new HabitCompletion { HabitId = habit.Id, Date = new DateOnly(2026, 8, 23), Outcome = HabitOutcome.Done, IsDone = true });
        await fixture.Context.SaveChangesAsync();

        await using var exported = new MemoryStream();
        await fixture.Service.WriteJsonExportAsync(exported);
        exported.Position = 0;
        var preview = await fixture.Service.StageJsonImportAsync(exported, exported.Length);
        Assert.AreEqual(1, preview.HabitCount);
        Assert.AreEqual(1, preview.OutcomeCount);

        fixture.Context.HabitCompletions.RemoveRange(fixture.Context.HabitCompletions);
        fixture.Context.Habits.RemoveRange(fixture.Context.Habits);
        await fixture.Context.SaveChangesAsync();
        await fixture.Service.ImportJsonAsync(preview.Token);
        Assert.AreEqual("Read", (await fixture.Context.Habits.SingleAsync()).Name);
        Assert.AreEqual(HabitOutcome.Done, (await fixture.Context.HabitCompletions.SingleAsync()).Outcome);
        Assert.IsGreaterThanOrEqualTo(1, Directory.GetFiles(fixture.Paths.BackupsPath, "*pre-import*.db").Length);
    }

    [TestMethod]
    public async Task InvalidJson_DoesNotChangeExistingData()
    {
        await using var fixture = await BackupFixture.CreateAsync();
        fixture.Context.Habits.Add(new Habit { Name = "Keep me", StartDate = new DateOnly(2026, 8, 1) });
        await fixture.Context.SaveChangesAsync();
        await using var invalid = new MemoryStream(Encoding.UTF8.GetBytes("{\"formatVersion\":99}"));
        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => fixture.Service.StageJsonImportAsync(invalid, invalid.Length));
        Assert.AreEqual("Keep me", (await fixture.Context.Habits.SingleAsync()).Name);
    }

    [TestMethod]
    public async Task SqliteBackupRestore_ReplacesOnlyWithValidDatabase()
    {
        await using var fixture = await BackupFixture.CreateAsync();
        var habit = new Habit { Name = "Before", StartDate = new DateOnly(2026, 8, 1) };
        fixture.Context.Habits.Add(habit); await fixture.Context.SaveChangesAsync();
        var backupPath = await fixture.Service.CreateDatabaseBackupAsync("test");
        habit.Name = "After"; await fixture.Context.SaveChangesAsync();
        await using var backup = File.OpenRead(backupPath);
        await fixture.Service.RestoreDatabaseAsync(backup, backup.Length);
        fixture.Context.ChangeTracker.Clear();
        Assert.AreEqual("Before", (await fixture.Context.Habits.SingleAsync()).Name);
    }

    private sealed class BackupFixture : IAsyncDisposable
    {
        private BackupFixture(string root, AppPaths paths, AppDbContext context, BackupService service) { Root = root; Paths = paths; Context = context; Service = service; }
        public string Root { get; }
        public AppPaths Paths { get; }
        public AppDbContext Context { get; }
        public BackupService Service { get; }

        public static async Task<BackupFixture> CreateAsync()
        {
            var root = Path.Combine(Path.GetTempPath(), $"FunHabitTracker-backup-{Guid.NewGuid():N}");
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["FunHabitTracker:DataDirectory"] = root, ["FunHabitTracker:MaxImportBytes"] = "5242880" }).Build();
            var paths = new AppPaths(configuration); paths.EnsureCreated();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={paths.DatabasePath};Foreign Keys=True").Options;
            var context = new AppDbContext(options); await context.Database.MigrateAsync();
            var clock = new TestClock(new DateTime(2026, 8, 23, 12, 0, 0));
            return new BackupFixture(root, paths, context, new BackupService(context, paths, clock, configuration));
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync(); SqliteConnection.ClearAllPools();
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }
    }
}

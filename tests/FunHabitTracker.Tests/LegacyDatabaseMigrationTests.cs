using FunHabitTracker.Data;
using FunHabitTracker.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunHabitTracker.Tests;

[TestClass]
public sealed class LegacyDatabaseMigrationTests
{
    [TestMethod]
    public async Task Initializer_AdoptsEnsureCreatedDatabaseAndPreservesDoneRows()
    {
        var root = Path.Combine(Path.GetTempPath(), $"FunHabitTracker-migration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "habits.db");
            await CreateLegacyDatabaseAsync(path);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["FunHabitTracker:DataDirectory"] = root }).Build();
            var paths = new AppPaths(configuration); paths.EnsureCreated();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={path};Foreign Keys=True").Options;
            await using var context = new AppDbContext(options);
            await new DatabaseInitializer(
                context,
                paths,
                NullLogger<DatabaseInitializer>.Instance,
                new TestHostEnvironment(root),
                configuration).InitializeAsync();
            var habit = await context.Habits.SingleAsync();
            Assert.AreEqual(new DateOnly(2025, 11, 15), habit.StartDate);
            Assert.AreEqual(1, await context.HabitCompletions.CountAsync());
            Assert.IsGreaterThanOrEqualTo(1, Directory.GetFiles(paths.BackupsPath, "*.db").Length);
        }
        finally { SqliteConnection.ClearAllPools(); if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [TestMethod]
    public async Task Initializer_ImportsPopulatedLegacyLocationWhenStableDatabaseIsEmpty()
    {
        var root = Path.Combine(Path.GetTempPath(), $"FunHabitTracker-location-{Guid.NewGuid():N}");
        var stableRoot = Path.Combine(root, "stable");
        var legacyRoot = Path.Combine(root, "legacy");
        Directory.CreateDirectory(stableRoot);
        Directory.CreateDirectory(legacyRoot);
        try
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["FunHabitTracker:DataDirectory"] = stableRoot,
                    ["FunHabitTracker:ImportLegacyDatabaseLocation"] = "true"
                }).Build();
            var paths = new AppPaths(configuration);
            paths.EnsureCreated();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={paths.DatabasePath};Foreign Keys=True")
                .Options;

            await using (var emptyContext = new AppDbContext(options))
            {
                await emptyContext.Database.MigrateAsync();
            }

            var legacyPath = Path.Combine(legacyRoot, "habits.db");
            await CreateLegacyDatabaseAsync(legacyPath);

            await using var context = new AppDbContext(options);
            var initializer = new DatabaseInitializer(
                context,
                paths,
                NullLogger<DatabaseInitializer>.Instance,
                new TestHostEnvironment(legacyRoot),
                configuration);
            await initializer.InitializeAsync();

            Assert.AreEqual("Legacy", (await context.Habits.SingleAsync()).Name);
            Assert.IsTrue(File.Exists(legacyPath));
            Assert.IsGreaterThanOrEqualTo(1, Directory.GetFiles(paths.BackupsPath, "*.db").Length);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static async Task CreateLegacyDatabaseAsync(string path)
    {
        await using var connection = new SqliteConnection($"Data Source={path}"); await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA foreign_keys=ON;
            CREATE TABLE Habits (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Description TEXT NULL, Frequency TEXT NOT NULL, IsActive INTEGER NOT NULL, CreatedAt TEXT NOT NULL);
            CREATE TABLE HabitCompletions (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, HabitId INTEGER NOT NULL, Date TEXT NOT NULL, IsDone INTEGER NOT NULL, FOREIGN KEY (HabitId) REFERENCES Habits(Id) ON DELETE CASCADE);
            CREATE UNIQUE INDEX IX_HabitCompletions_HabitId_Date ON HabitCompletions(HabitId, Date);
            INSERT INTO Habits (Name, Description, Frequency, IsActive, CreatedAt) VALUES ('Legacy', NULL, 'Weekly', 1, '2025-11-15 08:00:00');
            INSERT INTO HabitCompletions (HabitId, Date, IsDone) VALUES (1, '2026-08-22', 1), (1, '2026-08-23', 0);
            """;
        await command.ExecuteNonQueryAsync();
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "FunHabitTracker.Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

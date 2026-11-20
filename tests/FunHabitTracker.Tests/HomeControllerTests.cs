using System;
using System.Linq;
using FunHabitTracker.Controllers;
using FunHabitTracker.Data;
using FunHabitTracker.Models;
using FunHabitTracker.Services;
using FunHabitTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunHabitTracker.Tests;

[TestClass]
public class HomeControllerTests
{
    private SqliteConnection connection;
    private AppDbContext db;
    private HomeController controller;

    [TestInitialize]
    public void Setup()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        controller = new HomeController(new HabitService(db), db);
    }

    [TestCleanup]
    public void Cleanup() { db.Dispose(); connection.Dispose(); }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Toggle_UnknownOrInactiveHabit_RejectsWithoutWriting(bool inactiveExists)
    {
        var id = 999;
        if (inactiveExists)
        {
            var habit = new Habit { Name = "Archived", IsActive = false };
            db.Habits.Add(habit); db.SaveChanges(); id = habit.Id;
        }
        Assert.IsInstanceOfType(controller.ToggleToday(id), typeof(NotFoundResult));
        Assert.AreEqual(0, db.HabitCompletions.Count());
    }
    [TestMethod]
    public void Toggle_Twice_PreservesHistoryAndUsesOneRowToday()
    {
        var habit = new Habit { Name = "Read" };
        db.Habits.Add(habit); db.SaveChanges();
        var yesterday = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
        db.HabitCompletions.Add(new HabitCompletion { HabitId = habit.Id, Date = yesterday, IsDone = true });
        db.SaveChanges();
        controller.ToggleToday(habit.Id);
        Assert.IsTrue(db.HabitCompletions.Single(c => c.Date != yesterday).IsDone);
        controller.ToggleToday(habit.Id);
        db.ChangeTracker.Clear();
        Assert.AreEqual(2, db.HabitCompletions.Count());
        Assert.IsFalse(db.HabitCompletions.Single(c => c.Date != yesterday).IsDone);
        Assert.IsTrue(db.HabitCompletions.Single(c => c.Date == yesterday).IsDone);
    }

    [TestMethod]
    public void Toggle_DeletedHabit_ReturnsNotFound()
    {
        var habit = new Habit { Name = "Read" };
        db.Habits.Add(habit); db.SaveChanges();
        var id = habit.Id;
        db.Habits.Remove(habit); db.SaveChanges();
        Assert.IsInstanceOfType(controller.ToggleToday(id), typeof(NotFoundResult));
        Assert.AreEqual(0, db.HabitCompletions.Count());
    }
    [TestMethod]
    public void ExistingEf6Database_RetainsDataAndAcceptsNewCompletions()
    {
        // Schema exported with EF Core 6.0.0 from the reviewed AppDbContext.
        db.Database.EnsureDeleted();
        using var stream = typeof(HomeControllerTests).Assembly.GetManifestResourceStream("FunHabitTracker.Tests.Fixtures.Schema6.sql");
        using var reader = new System.IO.StreamReader(stream);
        db.Database.ExecuteSqlRaw(reader.ReadToEnd());
        db.Database.ExecuteSqlRaw("INSERT INTO Habits (Id, Name, Frequency, IsActive, CreatedAt) VALUES (7, 'Legacy', 'Daily', 1, '2000-01-01 00:00:00')");
        db.Database.ExecuteSqlRaw("INSERT INTO HabitCompletions (HabitId, Date, IsDone) VALUES (7, '2000-01-01', 1)");
        Assert.IsFalse(db.Database.EnsureCreated());
        Assert.AreEqual("Legacy", db.Habits.Single().Name);
        Assert.AreEqual(new DateOnly(2000, 1, 1), db.HabitCompletions.Single().Date);
        controller.ToggleToday(7);
        Assert.AreEqual(2, db.HabitCompletions.Count());
        Assert.IsTrue(db.HabitCompletions.Single(c => c.Date == new DateOnly(2000, 1, 1)).IsDone);
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
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
    private readonly List<string> commands = new();
    private SqliteConnection connection;
    private AppDbContext db;
    private HomeController controller;

    [TestInitialize]
    public void Setup()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).LogTo(commands.Add, LogLevel.Information).Options);
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
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(10)]
    public void Today_QueryCountIsBounded(int count)
    {
        for (var i = 0; i < count; i++) db.Habits.Add(new Habit { Name = "Habit " + i });
        db.Habits.Add(new Habit { Name = "Archived", IsActive = false });
        db.SaveChanges();
        commands.Clear();
        var model = (TodayViewModel)((ViewResult)controller.Today()).Model;
        Assert.AreEqual(count, model.Habits.Count);
        var queries = commands.Count(c => c.Contains("Executed DbCommand"));
        Assert.IsTrue(queries <= 2, "Expected at most two SQL commands, actual: " + queries);
    }
    [TestMethod]
    public void Today_ProjectsOnlyActiveHabitsAndTodaysState()
    {
        var done = new Habit { Name = "Done", Description = "Details" };
        var pending = new Habit { Name = "Pending" };
        var archived = new Habit { Name = "Archived", IsActive = false };
        db.Habits.AddRange(done, pending, archived); db.SaveChanges();
        var today = DateOnly.FromDateTime(DateTime.Today);
        db.HabitCompletions.AddRange(
            new HabitCompletion { HabitId = done.Id, Date = today, IsDone = true },
            new HabitCompletion { HabitId = pending.Id, Date = today.AddDays(-1), IsDone = true },
            new HabitCompletion { HabitId = archived.Id, Date = today, IsDone = true });
        db.SaveChanges();
        var model = (TodayViewModel)((ViewResult)controller.Today()).Model;
        Assert.AreEqual(2, model.Habits.Count);
        Assert.AreEqual(today, model.Date);
        Assert.IsTrue(model.Habits.Single(h => h.HabitId == done.Id).IsDoneToday);
        Assert.AreEqual("Details", model.Habits.Single(h => h.HabitId == done.Id).Description);
        Assert.IsFalse(model.Habits.Single(h => h.HabitId == pending.Id).IsDoneToday);
    }
}

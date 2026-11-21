using FunHabitTracker.Models;
using FunHabitTracker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunHabitTracker.Tests;

[TestClass]
public sealed class DailyTrackingServiceTests
{
    [TestMethod]
    public async Task SetOutcome_IsIdempotentAndUsesOneRow()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var habit = new Habit { Name = "Read", StartDate = new DateOnly(2026, 8, 1), IsActive = true };
        database.Context.Habits.Add(habit); await database.Context.SaveChangesAsync();
        var service = CreateService(database);
        Assert.IsTrue(await service.SetOutcomeAsync(habit.Id, new DateOnly(2026, 8, 23), HabitOutcome.Done));
        Assert.IsTrue(await service.SetOutcomeAsync(habit.Id, new DateOnly(2026, 8, 23), HabitOutcome.Done));
        Assert.AreEqual(1, await database.Context.HabitCompletions.CountAsync());
    }

    [TestMethod]
    public async Task SetOutcome_RejectsUnknownOrArchivedHabit()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var service = CreateService(database);
        Assert.IsFalse(await service.SetOutcomeAsync(404, new DateOnly(2026, 8, 23), HabitOutcome.Done));
    }

    [TestMethod]
    public async Task ToggleOutcome_CoversEveryTransitionWithOneOrZeroRows()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var habit = new Habit { Name = "Read", StartDate = new DateOnly(2026, 8, 1), IsActive = true };
        database.Context.Habits.Add(habit);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database);
        var date = new DateOnly(2026, 8, 23);

        Assert.IsTrue(await service.ToggleOutcomeAsync(habit.Id, date, HabitOutcome.Done));
        Assert.AreEqual(HabitOutcome.Done, (await database.Context.HabitCompletions.SingleAsync()).Outcome);

        Assert.IsTrue(await service.ToggleOutcomeAsync(habit.Id, date, HabitOutcome.Skipped));
        Assert.AreEqual(HabitOutcome.Skipped, (await database.Context.HabitCompletions.SingleAsync()).Outcome);
        Assert.AreEqual(1, await database.Context.HabitCompletions.CountAsync());

        Assert.IsTrue(await service.ToggleOutcomeAsync(habit.Id, date, HabitOutcome.Skipped));
        Assert.AreEqual(0, await database.Context.HabitCompletions.CountAsync());

        Assert.IsTrue(await service.ToggleOutcomeAsync(habit.Id, date, HabitOutcome.Done));
        Assert.IsTrue(await service.ToggleOutcomeAsync(habit.Id, date, HabitOutcome.Done));
        Assert.AreEqual(0, await database.Context.HabitCompletions.CountAsync());
    }

    [TestMethod]
    public async Task ToggleOutcome_RejectsUnknownArchivedAndUnscheduledHabits()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var habit = new Habit
        {
            Name = "Monday",
            StartDate = new DateOnly(2026, 8, 1),
            RecurrenceType = RecurrenceType.SelectedWeekdays,
            ScheduledDays = ScheduledDays.Monday,
            IsActive = true
        };
        database.Context.Habits.Add(habit);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database);

        Assert.IsFalse(await service.ToggleOutcomeAsync(404, new DateOnly(2026, 8, 24), HabitOutcome.Done));
        Assert.IsFalse(await service.ToggleOutcomeAsync(habit.Id, new DateOnly(2026, 8, 25), HabitOutcome.Done));
        habit.IsArchived = true;
        await database.Context.SaveChangesAsync();
        Assert.IsFalse(await service.ToggleOutcomeAsync(habit.Id, new DateOnly(2026, 8, 24), HabitOutcome.Done));
        Assert.AreEqual(0, await database.Context.HabitCompletions.CountAsync());
    }

    [TestMethod]
    public async Task Today_OnlyContainsDueHabits()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        database.Context.Habits.AddRange(
            new Habit { Name = "Monday", StartDate = new DateOnly(2026, 8, 1), RecurrenceType = RecurrenceType.SelectedWeekdays, ScheduledDays = ScheduledDays.Monday, IsActive = true },
            new Habit { Name = "Tuesday", StartDate = new DateOnly(2026, 8, 1), RecurrenceType = RecurrenceType.SelectedWeekdays, ScheduledDays = ScheduledDays.Tuesday, IsActive = true });
        await database.Context.SaveChangesAsync();
        var dashboard = await CreateService(database).GetTodayAsync(new DateOnly(2026, 8, 24));
        Assert.AreEqual(1, dashboard.Habits.Count);
        Assert.AreEqual("Monday", dashboard.Habits[0].Name);
    }

    private static DailyTrackingService CreateService(SqliteTestDatabase database)
    {
        var recurrence = new RecurrenceService();
        return new DailyTrackingService(database.Context, recurrence, new StreakCalculator(recurrence));
    }
}

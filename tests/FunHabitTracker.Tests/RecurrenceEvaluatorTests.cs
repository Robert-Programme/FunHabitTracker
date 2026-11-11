using FunHabitTracker.Models;
using FunHabitTracker.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunHabitTracker.Tests;

[TestClass]
public sealed class RecurrenceEvaluatorTests
{
    private readonly RecurrenceService _service = new();

    [TestMethod]
    public void DailyHabit_IsNotDueBeforeStartDate()
    {
        var habit = ActiveHabit(new DateOnly(2026, 8, 24), RecurrenceType.Daily, ScheduledDays.EveryDay);
        Assert.IsFalse(_service.IsDue(habit, new DateOnly(2026, 8, 23)));
        Assert.IsTrue(_service.IsDue(habit, new DateOnly(2026, 8, 24)));
    }

    [TestMethod]
    public void SelectedWeekdays_OnlyMatchesConfiguredDays()
    {
        var habit = ActiveHabit(new DateOnly(2026, 8, 1), RecurrenceType.SelectedWeekdays, ScheduledDays.Monday | ScheduledDays.Wednesday | ScheduledDays.Friday);
        Assert.IsTrue(_service.IsDue(habit, new DateOnly(2026, 8, 24)));
        Assert.IsFalse(_service.IsDue(habit, new DateOnly(2026, 8, 25)));
    }

    [TestMethod]
    public void ArchivedHabit_IsNeverDue()
    {
        var habit = ActiveHabit(new DateOnly(2026, 8, 1), RecurrenceType.Daily, ScheduledDays.EveryDay);
        habit.IsArchived = true;
        Assert.IsFalse(_service.IsDue(habit, new DateOnly(2026, 8, 24)));
    }

    private static Habit ActiveHabit(DateOnly startDate, RecurrenceType type, ScheduledDays days) => new() { Name = "Test", StartDate = startDate, RecurrenceType = type, ScheduledDays = days, IsActive = true };
}

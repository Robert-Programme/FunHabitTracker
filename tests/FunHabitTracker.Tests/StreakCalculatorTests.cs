using FunHabitTracker.Models;
using FunHabitTracker.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunHabitTracker.Tests;

[TestClass]
public sealed class StreakCalculatorTests
{
    private readonly StreakCalculator _calculator = new(new RecurrenceService());

    [TestMethod]
    public void Streak_IgnoresWeekendForWeekdayHabit()
    {
        var habit = WeekdayHabit();
        var outcomes = new Dictionary<DateOnly, HabitOutcome> { [new(2026, 8, 21)] = HabitOutcome.Done, [new(2026, 8, 24)] = HabitOutcome.Done };
        Assert.AreEqual(2, _calculator.Calculate(habit, outcomes, new DateOnly(2026, 8, 24)));
    }

    [TestMethod]
    public void Streak_SkipDoesNotExtendOrBreak()
    {
        var habit = WeekdayHabit();
        var outcomes = new Dictionary<DateOnly, HabitOutcome> { [new(2026, 8, 20)] = HabitOutcome.Done, [new(2026, 8, 21)] = HabitOutcome.Skipped, [new(2026, 8, 24)] = HabitOutcome.Done };
        Assert.AreEqual(2, _calculator.Calculate(habit, outcomes, new DateOnly(2026, 8, 24)));
    }

    [TestMethod]
    public void Streak_MissedScheduledDayBreaks()
    {
        var habit = WeekdayHabit();
        var outcomes = new Dictionary<DateOnly, HabitOutcome> { [new(2026, 8, 20)] = HabitOutcome.Done, [new(2026, 8, 24)] = HabitOutcome.Done };
        Assert.AreEqual(1, _calculator.Calculate(habit, outcomes, new DateOnly(2026, 8, 24)));
    }

    [TestMethod]
    public void Streak_RemainsComputableAfterArchiving()
    {
        var habit = WeekdayHabit(); habit.IsArchived = true; habit.IsActive = false;
        var outcomes = new Dictionary<DateOnly, HabitOutcome> { [new(2026, 8, 21)] = HabitOutcome.Done, [new(2026, 8, 24)] = HabitOutcome.Done };
        Assert.AreEqual(2, _calculator.Calculate(habit, outcomes, new DateOnly(2026, 8, 24)));
    }

    private static Habit WeekdayHabit() => new() { Name = "Weekday", StartDate = new DateOnly(2026, 8, 1), RecurrenceType = RecurrenceType.SelectedWeekdays, ScheduledDays = ScheduledDays.Monday | ScheduledDays.Tuesday | ScheduledDays.Wednesday | ScheduledDays.Thursday | ScheduledDays.Friday, IsActive = true };
}

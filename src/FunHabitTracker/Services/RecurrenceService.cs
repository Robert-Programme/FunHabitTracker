using FunHabitTracker.Models;

namespace FunHabitTracker.Services;

public sealed class RecurrenceService
{
    public bool IsDue(Habit habit, DateOnly date)
    {
        return !habit.IsArchived && habit.IsActive && IsScheduled(habit, date);
    }

    public bool IsScheduled(Habit habit, DateOnly date)
    {
        if (date < habit.StartDate) return false;
        return habit.RecurrenceType switch
        {
            RecurrenceType.Daily => true,
            RecurrenceType.SelectedWeekdays => habit.ScheduledDays.HasFlag(ToScheduledDay(date.DayOfWeek)),
            _ => false
        };
    }

    public string FormatSchedule(Habit habit)
    {
        if (habit.RecurrenceType == RecurrenceType.Daily) return "Taeglich";
        var labels = new[] { (ScheduledDays.Monday, "Mo"), (ScheduledDays.Tuesday, "Di"), (ScheduledDays.Wednesday, "Mi"), (ScheduledDays.Thursday, "Do"), (ScheduledDays.Friday, "Fr"), (ScheduledDays.Saturday, "Sa"), (ScheduledDays.Sunday, "So") };
        return string.Join(", ", labels.Where(item => habit.ScheduledDays.HasFlag(item.Item1)).Select(item => item.Item2));
    }

    private static ScheduledDays ToScheduledDay(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => ScheduledDays.Monday,
        DayOfWeek.Tuesday => ScheduledDays.Tuesday,
        DayOfWeek.Wednesday => ScheduledDays.Wednesday,
        DayOfWeek.Thursday => ScheduledDays.Thursday,
        DayOfWeek.Friday => ScheduledDays.Friday,
        DayOfWeek.Saturday => ScheduledDays.Saturday,
        DayOfWeek.Sunday => ScheduledDays.Sunday,
        _ => ScheduledDays.None
    };
}

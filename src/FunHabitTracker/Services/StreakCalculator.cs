using FunHabitTracker.Models;

namespace FunHabitTracker.Services;

public sealed class StreakCalculator
{
    private readonly RecurrenceService _recurrence;
    public StreakCalculator(RecurrenceService recurrence) => _recurrence = recurrence;

    public int Calculate(Habit habit, IReadOnlyDictionary<DateOnly, HabitOutcome> outcomes, DateOnly throughDate)
    {
        var streak = 0;
        var date = throughDate;
        var inspected = 0;
        while (date >= habit.StartDate && inspected++ < 3660)
        {
            if (!_recurrence.IsScheduled(habit, date)) { date = date.AddDays(-1); continue; }
            if (outcomes.TryGetValue(date, out var outcome))
            {
                if (outcome == HabitOutcome.Done) streak++;
                date = date.AddDays(-1);
                continue;
            }
            if (date == throughDate) { date = date.AddDays(-1); continue; }
            break;
        }
        return streak;
    }
}

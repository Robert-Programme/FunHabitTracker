using FunHabitTracker.Data;
using FunHabitTracker.Models;
using FunHabitTracker.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FunHabitTracker.Services;

public sealed class DailyTrackingService
{
    private readonly AppDbContext _context;
    private readonly RecurrenceService _recurrence;
    private readonly StreakCalculator _streaks;
    public DailyTrackingService(AppDbContext context, RecurrenceService recurrence, StreakCalculator streaks) { _context = context; _recurrence = recurrence; _streaks = streaks; }

    public async Task<TodayDashboardModel> GetTodayAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var habits = await _context.Habits.AsNoTracking().Where(habit => !habit.IsArchived && habit.IsActive).ToListAsync(cancellationToken);
        var dueHabits = habits.Where(habit => _recurrence.IsDue(habit, date)).OrderBy(habit => habit.Name).ToArray();
        var ids = dueHabits.Select(habit => habit.Id).ToArray();
        var allOutcomes = await _context.HabitCompletions.AsNoTracking().Where(completion => ids.Contains(completion.HabitId) && completion.Date <= date).ToListAsync(cancellationToken);
        return new TodayDashboardModel { Date = date, Habits = dueHabits.Select(habit => { var outcomes = allOutcomes.Where(item => item.HabitId == habit.Id).ToDictionary(item => item.Date, item => item.Outcome); outcomes.TryGetValue(date, out var outcome); return new TodayHabitModel(habit.Id, habit.Name, habit.Description, outcome == HabitOutcome.Done ? TodayStatus.Done : outcome == HabitOutcome.Skipped ? TodayStatus.Skipped : TodayStatus.Pending, _streaks.Calculate(habit, outcomes, date)); }).ToArray() };
    }

    public async Task<bool> SetOutcomeAsync(int habitId, DateOnly date, HabitOutcome outcome, CancellationToken cancellationToken = default)
    {
        var habit = await _context.Habits.SingleOrDefaultAsync(item => item.Id == habitId, cancellationToken);
        if (habit is null || !_recurrence.IsDue(habit, date)) return false;
        var completion = await _context.HabitCompletions.SingleOrDefaultAsync(item => item.HabitId == habitId && item.Date == date, cancellationToken);
        if (completion is null) _context.HabitCompletions.Add(new HabitCompletion { HabitId = habitId, Date = date, Outcome = outcome, IsDone = outcome == HabitOutcome.Done });
        else { completion.Outcome = outcome; completion.IsDone = outcome == HabitOutcome.Done; }
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 19 })
        {
            _context.ChangeTracker.Clear();
            return await _context.HabitCompletions.AnyAsync(item => item.HabitId == habitId && item.Date == date, cancellationToken);
        }
    }

    public async Task<bool> ToggleOutcomeAsync(int habitId, DateOnly date, HabitOutcome outcome, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(outcome)) return false;
        var habit = await _context.Habits.SingleOrDefaultAsync(item => item.Id == habitId, cancellationToken);
        if (habit is null || !_recurrence.IsDue(habit, date)) return false;
        var completion = await _context.HabitCompletions.SingleOrDefaultAsync(item => item.HabitId == habitId && item.Date == date, cancellationToken);
        if (completion?.Outcome == outcome)
        {
            _context.HabitCompletions.Remove(completion);
        }
        else if (completion is null)
        {
            _context.HabitCompletions.Add(new HabitCompletion { HabitId = habitId, Date = date, Outcome = outcome, IsDone = outcome == HabitOutcome.Done });
        }
        else
        {
            completion.Outcome = outcome;
            completion.IsDone = outcome == HabitOutcome.Done;
        }
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ClearOutcomeAsync(int habitId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var completion = await _context.HabitCompletions.SingleOrDefaultAsync(item => item.HabitId == habitId && item.Date == date, cancellationToken);
        if (completion is null) return await _context.Habits.AnyAsync(item => item.Id == habitId && !item.IsArchived, cancellationToken);
        _context.HabitCompletions.Remove(completion); await _context.SaveChangesAsync(cancellationToken); return true;
    }

    public async Task<HabitHistoryModel?> GetHistoryAsync(int habitId, DateOnly throughDate, int days = 42, CancellationToken cancellationToken = default)
    {
        var habit = await _context.Habits.AsNoTracking().SingleOrDefaultAsync(item => item.Id == habitId, cancellationToken);
        if (habit is null) return null;
        var firstDate = throughDate.AddDays(-(days - 1));
        var completions = await _context.HabitCompletions.AsNoTracking().Where(item => item.HabitId == habitId && item.Date >= firstDate && item.Date <= throughDate).ToDictionaryAsync(item => item.Date, item => item.Outcome, cancellationToken);
        var allCompletions = await _context.HabitCompletions.AsNoTracking().Where(item => item.HabitId == habitId && item.Date <= throughDate).ToListAsync(cancellationToken);
        var listItem = new HabitListItemModel(habit.Id, habit.Name, habit.Description, _recurrence.FormatSchedule(habit), habit.StartDate, habit.IsArchived, _streaks.Calculate(habit, allCompletions.ToDictionary(item => item.Date, item => item.Outcome), throughDate));
        return new HabitHistoryModel { Habit = listItem, Days = Enumerable.Range(0, days).Select(offset => firstDate.AddDays(offset)).Select(date => { completions.TryGetValue(date, out var outcome); return new HistoryDayModel(date, _recurrence.IsScheduled(habit, date), outcome == HabitOutcome.Done ? TodayStatus.Done : outcome == HabitOutcome.Skipped ? TodayStatus.Skipped : TodayStatus.Pending); }).ToArray() };
    }
}

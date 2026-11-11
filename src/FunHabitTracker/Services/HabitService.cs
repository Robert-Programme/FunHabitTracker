using FunHabitTracker.Data;
using FunHabitTracker.Infrastructure;
using FunHabitTracker.Models;
using FunHabitTracker.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FunHabitTracker.Services;

public sealed class HabitService
{
    private readonly AppDbContext _context;
    private readonly IClock _clock;
    private readonly RecurrenceService _recurrence;
    private readonly StreakCalculator _streaks;

    public HabitService(AppDbContext context)
        : this(context, new SystemClock(), new RecurrenceService(), new StreakCalculator(new RecurrenceService()))
    {
    }

    public HabitService(AppDbContext context, IClock clock, RecurrenceService recurrence, StreakCalculator streaks)
    {
        _context = context; _clock = clock; _recurrence = recurrence; _streaks = streaks;
    }

    public async Task<IReadOnlyList<HabitListItemModel>> ListAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        var habits = await _context.Habits.AsNoTracking().OrderBy(habit => habit.IsArchived).ThenBy(habit => habit.Name).ToListAsync(cancellationToken);
        var ids = habits.Select(habit => habit.Id).ToArray();
        var completions = await _context.HabitCompletions.AsNoTracking().Where(completion => ids.Contains(completion.HabitId) && completion.Date <= today).ToListAsync(cancellationToken);
        return habits.Select(habit => ToListItem(habit, completions, today)).ToArray();
    }

    public Task<HabitInputModel?> GetInputAsync(int id, CancellationToken cancellationToken = default) => _context.Habits.AsNoTracking().Where(habit => habit.Id == id).Select(habit => new HabitInputModel { Id = habit.Id, Name = habit.Name, Description = habit.Description, StartDate = habit.StartDate, RecurrenceType = habit.RecurrenceType, ScheduledDays = habit.ScheduledDays }).SingleOrDefaultAsync(cancellationToken);

    public async Task<int> CreateAsync(HabitInputModel input, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var habit = new Habit { Name = input.Name.Trim(), Description = NormalizeOptional(input.Description), StartDate = input.StartDate, RecurrenceType = input.RecurrenceType, ScheduledDays = NormalizeDays(input), Frequency = input.RecurrenceType == RecurrenceType.Daily ? "Daily" : "SelectedWeekdays", IsActive = true, IsArchived = false, CreatedAt = now, UpdatedAt = now };
        _context.Habits.Add(habit);
        await _context.SaveChangesAsync(cancellationToken);
        return habit.Id;
    }

    public async Task<bool> UpdateAsync(HabitInputModel input, CancellationToken cancellationToken = default)
    {
        var habit = await _context.Habits.SingleOrDefaultAsync(item => item.Id == input.Id, cancellationToken);
        if (habit is null) return false;
        habit.Name = input.Name.Trim(); habit.Description = NormalizeOptional(input.Description); habit.StartDate = input.StartDate; habit.RecurrenceType = input.RecurrenceType; habit.ScheduledDays = NormalizeDays(input); habit.Frequency = input.RecurrenceType == RecurrenceType.Daily ? "Daily" : "SelectedWeekdays"; habit.UpdatedAt = _clock.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> ArchiveAsync(int id, CancellationToken cancellationToken = default) => SetArchivedAsync(id, true, cancellationToken);
    public Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default) => SetArchivedAsync(id, false, cancellationToken);
    public List<Habit> GetAllHabits() => _context.Habits.AsNoTracking().ToList();
    public Habit? GetHabitById(int id) => _context.Habits.Find(id);
    public void AddHabit(Habit habit) { _context.Habits.Add(habit); _context.SaveChanges(); }
    public void UpdateHabit(Habit habit) { _context.Habits.Update(habit); _context.SaveChanges(); }
    public void DeleteHabit(int id) { var habit = GetHabitById(id); if (habit is null) return; _context.Habits.Remove(habit); _context.SaveChanges(); }

    private async Task<bool> SetArchivedAsync(int id, bool archived, CancellationToken cancellationToken)
    {
        var habit = await _context.Habits.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (habit is null) return false;
        habit.IsArchived = archived; habit.IsActive = !archived; habit.ArchivedAt = archived ? _clock.UtcNow : null; habit.UpdatedAt = _clock.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private HabitListItemModel ToListItem(Habit habit, IReadOnlyCollection<HabitCompletion> completions, DateOnly today)
    {
        var outcomes = completions.Where(item => item.HabitId == habit.Id).ToDictionary(item => item.Date, item => item.Outcome);
        return new HabitListItemModel(habit.Id, habit.Name, habit.Description, _recurrence.FormatSchedule(habit), habit.StartDate, habit.IsArchived, _streaks.Calculate(habit, outcomes, today));
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ScheduledDays NormalizeDays(HabitInputModel input) => input.RecurrenceType == RecurrenceType.Daily ? ScheduledDays.EveryDay : input.ScheduledDays;
}

using System.ComponentModel.DataAnnotations;
using FunHabitTracker.Models;

namespace FunHabitTracker.ViewModels;

public sealed class HabitInputModel : IValidatableObject
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Bitte gib deinem Habit einen Namen.")]
    [MaxLength(200, ErrorMessage = "Der Name darf hoechstens 200 Zeichen lang sein.")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;
    [MaxLength(1000, ErrorMessage = "Die Beschreibung darf hoechstens 1000 Zeichen lang sein.")]
    [Display(Name = "Beschreibung")]
    public string? Description { get; set; }
    [Display(Name = "Startdatum")]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Display(Name = "Wiederholung")]
    public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.Daily;
    public ScheduledDays ScheduledDays { get; set; } = ScheduledDays.EveryDay;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("Bitte gib deinem Habit einen Namen.", new[] { nameof(Name) });
        }
        if (RecurrenceType == RecurrenceType.SelectedWeekdays && ScheduledDays == ScheduledDays.None)
        {
            yield return new ValidationResult("Waehle mindestens einen Wochentag.", new[] { nameof(ScheduledDays) });
        }
    }
}

public sealed record HabitListItemModel(int Id, string Name, string? Description, string ScheduleLabel, DateOnly StartDate, bool IsArchived, int CurrentStreak);

public enum TodayStatus { Pending = 0, Done = 1, Skipped = 2 }

public sealed record TodayHabitModel(int Id, string Name, string? Description, TodayStatus Status, int CurrentStreak);

public sealed class TodayDashboardModel
{
    public DateOnly Date { get; init; }
    public IReadOnlyList<TodayHabitModel> Habits { get; init; } = Array.Empty<TodayHabitModel>();
    public int DoneCount => Habits.Count(habit => habit.Status == TodayStatus.Done);
    public int SkippedCount => Habits.Count(habit => habit.Status == TodayStatus.Skipped);
    public int PendingCount => Habits.Count(habit => habit.Status == TodayStatus.Pending);
    public int DueCount => Habits.Count;
    public int CompletionPercent => DueCount == 0 ? 0 : (int)Math.Round(DoneCount * 100d / DueCount);
}

public sealed record HistoryDayModel(DateOnly Date, bool IsScheduled, TodayStatus Status);

public sealed class HabitHistoryModel
{
    public required HabitListItemModel Habit { get; init; }
    public IReadOnlyList<HistoryDayModel> Days { get; init; } = Array.Empty<HistoryDayModel>();
    public int DoneCount => Days.Count(day => day.Status == TodayStatus.Done);
    public int ScheduledCount => Days.Count(day => day.IsScheduled);
}

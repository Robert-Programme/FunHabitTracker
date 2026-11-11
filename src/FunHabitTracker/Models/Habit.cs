using System;
using System.ComponentModel.DataAnnotations;

#nullable enable

namespace FunHabitTracker.Models
{
    public enum RecurrenceType
    {
        Daily = 0,
        SelectedWeekdays = 1
    }

    [Flags]
    public enum ScheduledDays
    {
        None = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 4,
        Thursday = 8,
        Friday = 16,
        Saturday = 32,
        Sunday = 64,
        EveryDay = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
    }

    public class Habit
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Retained while legacy databases are adopted. New scheduling uses RecurrenceType.
        public string Frequency { get; set; } = "Daily";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateOnly StartDate { get; set; }

        public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.Daily;

        public ScheduledDays ScheduledDays { get; set; } = ScheduledDays.EveryDay;

        public bool IsArchived { get; set; }

        public DateTime? ArchivedAt { get; set; }

        public Habit()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = CreatedAt;
            StartDate = DateOnly.FromDateTime(DateTime.Today);
        }
    }
}

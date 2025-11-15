using System;
using System.Collections.Generic;

#nullable enable

namespace FunHabitTracker.ViewModels
{
    public class TodayHabitViewModel
    {
        public int HabitId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsDoneToday { get; set; }
    }

    public class TodayViewModel
    {
        public DateOnly Date { get; set; }
        public List<TodayHabitViewModel> Habits { get; set; } = new();
    }
}

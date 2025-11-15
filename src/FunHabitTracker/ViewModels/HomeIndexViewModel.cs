using System.Collections.Generic;
using FunHabitTracker.Models;

namespace FunHabitTracker.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<Habit> Habits { get; set; } = new List<Habit>();
    }
}

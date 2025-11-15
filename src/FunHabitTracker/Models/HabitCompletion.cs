using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable enable

namespace FunHabitTracker.Models
{
    public class HabitCompletion
    {
        public int Id { get; set; }
        
        [Required]
        public int HabitId { get; set; }
        
        [ForeignKey("HabitId")]
        public Habit? Habit { get; set; }
        
        [Required]
        public DateOnly Date { get; set; }
        
        public bool IsDone { get; set; }
    }
}

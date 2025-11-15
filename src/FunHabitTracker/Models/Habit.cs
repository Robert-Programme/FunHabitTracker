using System;
using System.ComponentModel.DataAnnotations;

#nullable enable

namespace FunHabitTracker.Models
{
    public class Habit
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public string Frequency { get; set; } = "Daily";
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; }

        public Habit()
        {
            CreatedAt = DateTime.Now;
        }
    }
}
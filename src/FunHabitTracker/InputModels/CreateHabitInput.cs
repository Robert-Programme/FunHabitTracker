using System.ComponentModel.DataAnnotations;
using FunHabitTracker.Models;

#nullable enable

namespace FunHabitTracker.InputModels;

public class CreateHabitInput
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Frequency { get; set; } = "Daily";
    public bool IsActive { get; set; } = true;

    public Habit ToHabit() => new Habit
    {
        Name = Name,
        Description = Description,
        Frequency = string.IsNullOrWhiteSpace(Frequency) ? "Daily" : Frequency.Trim(),
        IsActive = IsActive
    };
}

using System.Threading.Tasks;
using FunHabitTracker.Data;
using FunHabitTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FunHabitTracker.Controllers;

/// <summary>
/// Provides CRUD actions for managing habits. Currently supports listing and
/// creating new habits. Additional actions such as editing or deleting can be
/// added later.
/// </summary>
public class HabitsController : Controller
{
    private readonly AppDbContext _context;

    public HabitsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Displays a list of all habits in the system regardless of their active
    /// state.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var habits = await _context.Habits.OrderByDescending(h => h.CreatedAt).ToListAsync();
        return View(habits);
    }

    /// <summary>
    /// Renders a form for creating a new habit.
    /// </summary>
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Accepts and processes the submission of a new habit. If validation passes
    /// the habit is saved and the user is redirected to the list of habits.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description,IsActive")] Habit habit)
    {
        if (!ModelState.IsValid)
        {
            return View(habit);
        }

        habit.CreatedAt = DateTime.Now;
        _context.Habits.Add(habit);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}

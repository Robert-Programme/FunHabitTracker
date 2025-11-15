using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using FunHabitTracker.Services;
using FunHabitTracker.ViewModels;
using FunHabitTracker.Models;
using FunHabitTracker.Data;

namespace FunHabitTracker.Controllers
{
    public class HomeController : Controller
    {
        private readonly HabitService _habitService;
        private readonly AppDbContext _context;

        public HomeController(HabitService habitService, AppDbContext context)
        {
            _habitService = habitService;
            _context = context;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Today");
        }

        public IActionResult Today()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            
            // Get all active habits
            var activeHabits = _context.Habits
                .Where(h => h.IsActive)
                .ToList();

            var viewModel = new TodayViewModel
            {
                Date = today,
                Habits = activeHabits.Select(habit =>
                {
                    var completion = _context.HabitCompletions
                        .FirstOrDefault(hc => hc.HabitId == habit.Id && hc.Date == today);

                    return new TodayHabitViewModel
                    {
                        HabitId = habit.Id,
                        Name = habit.Name,
                        Description = habit.Description,
                        IsDoneToday = completion?.IsDone ?? false
                    };
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleToday(int habitId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            
            var completion = _context.HabitCompletions
                .FirstOrDefault(hc => hc.HabitId == habitId && hc.Date == today);

            if (completion == null)
            {
                // Create new entry with IsDone = true
                completion = new HabitCompletion
                {
                    HabitId = habitId,
                    Date = today,
                    IsDone = true
                };
                _context.HabitCompletions.Add(completion);
            }
            else
            {
                // Toggle the IsDone status
                completion.IsDone = !completion.IsDone;
                _context.HabitCompletions.Update(completion);
            }

            _context.SaveChanges();

            return RedirectToAction("Today");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddHabit(string name, string description, string frequency)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Name is required");
            }

            var habit = new Habit
            {
                Name = name,
                Description = description,
                Frequency = frequency
            };

            _habitService.AddHabit(habit);

            return RedirectToAction("Today");
        }
    }
}
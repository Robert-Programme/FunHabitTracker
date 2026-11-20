using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using FunHabitTracker.Services;
using FunHabitTracker.ViewModels;
using FunHabitTracker.Models;
using FunHabitTracker.Data;
using FunHabitTracker.InputModels;

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

            var habits = (
                from habit in _context.Habits
                where habit.IsActive
                join completion in _context.HabitCompletions.Where(c => c.Date == today)
                    on habit.Id equals completion.HabitId into dailyCompletions
                from completion in dailyCompletions.DefaultIfEmpty()
                select new TodayHabitViewModel
                {
                    HabitId = habit.Id,
                    Name = habit.Name,
                    Description = habit.Description,
                    IsDoneToday = completion != null && completion.IsDone
                }).ToList();

            var viewModel = new TodayViewModel { Date = today, Habits = habits };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleToday(int habitId)
        {
            if (!_context.Habits.Any(h => h.Id == habitId && h.IsActive))
            {
                return NotFound();
            }

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
        public IActionResult AddHabit(CreateHabitInput input)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var habit = input.ToHabit();

            _habitService.AddHabit(habit);

            return RedirectToAction("Today");
        }
    }
}

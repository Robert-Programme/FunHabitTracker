using System.Collections.Generic;
using System.Linq;
using FunHabitTracker.Models;
using FunHabitTracker.Data;

namespace FunHabitTracker.Services
{
    public class HabitService
    {
        private readonly AppDbContext _context;

        public HabitService(AppDbContext context)
        {
            _context = context;
        }

        public List<Habit> GetAllHabits()
        {
            // Guard against the DbSet being null (e.g. if migrations/db not created yet)
            var habits = _context.Habits?.ToList();
            return habits ?? new List<Habit>();
        }

        public Habit GetHabitById(int id)
        {
            return _context.Habits.FirstOrDefault(h => h.Id == id);
        }

        public void AddHabit(Habit habit)
        {
            // Add using DbSet if available, otherwise use the context entry to add
            if (_context.Habits != null)
            {
                _context.Habits.Add(habit);
            }
            else
            {
                _context.Add(habit);
            }

            _context.SaveChanges();
        }

        public void UpdateHabit(Habit habit)
        {
            _context.Habits.Update(habit);
            _context.SaveChanges();
        }

        public void DeleteHabit(int id)
        {
            var habit = GetHabitById(id);
            if (habit != null)
            {
                _context.Habits.Remove(habit);
                _context.SaveChanges();
            }
        }
    }
}
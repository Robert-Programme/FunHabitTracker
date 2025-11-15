using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using FunHabitTracker.Data;
using FunHabitTracker.Models;
using FunHabitTracker.Services;

namespace FunHabitTracker.Tests
{
    [TestClass]
    public class HabitServiceTests
    {
        private HabitService _habitService;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            _habitService = new HabitService(context);
        }

        [TestMethod]
        public void AddHabit_ShouldAddHabit()
        {
            var habit = new Habit { Id = 1, Name = "Drink Water", Description = "Drink 2 liters of water daily", Frequency = "Daily" };
            _habitService.AddHabit(habit);

            var habits = _habitService.GetAllHabits();
            Assert.AreEqual(1, habits.Count());
            Assert.AreEqual("Drink Water", habits.First().Name);
        }

        [TestMethod]
        public void UpdateHabit_ShouldUpdateHabit()
        {
            var habit = new Habit { Id = 1, Name = "Exercise", Description = "30 minutes of exercise", Frequency = "Daily" };
            _habitService.AddHabit(habit);

            habit.Name = "Morning Exercise";
            _habitService.UpdateHabit(habit);

            var updatedHabit = _habitService.GetHabitById(1);
            Assert.AreEqual("Morning Exercise", updatedHabit.Name);
        }

        [TestMethod]
        public void DeleteHabit_ShouldRemoveHabit()
        {
            var habit = new Habit { Id = 1, Name = "Read a Book", Description = "Read for 30 minutes", Frequency = "Daily" };
            _habitService.AddHabit(habit);

            _habitService.DeleteHabit(1);
            var habits = _habitService.GetAllHabits();
            Assert.AreEqual(0, habits.Count());
        }

        [TestMethod]
        public void GetAllHabits_ShouldReturnAllHabits()
        {
            var habit1 = new Habit { Id = 1, Name = "Meditate", Description = "Meditate for 10 minutes", Frequency = "Daily" };
            var habit2 = new Habit { Id = 2, Name = "Journal", Description = "Write in journal", Frequency = "Daily" };
            _habitService.AddHabit(habit1);
            _habitService.AddHabit(habit2);

            var habits = _habitService.GetAllHabits();
            Assert.AreEqual(2, habits.Count());
        }
    }

    // Tests use an in-memory AppDbContext, so no custom MockDbContext/MockDbSet needed.
}
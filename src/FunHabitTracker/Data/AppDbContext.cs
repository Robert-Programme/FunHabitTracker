using Microsoft.EntityFrameworkCore;
using FunHabitTracker.Models;

namespace FunHabitTracker.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Habit> Habits { get; set; }
        public DbSet<HabitCompletion> HabitCompletions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Habit
            modelBuilder.Entity<Habit>()
                .Property(h => h.Name)
                .HasMaxLength(200)
                .IsRequired();

            // Configure HabitCompletion
            modelBuilder.Entity<HabitCompletion>()
                .HasOne(hc => hc.Habit)
                .WithMany()
                .HasForeignKey(hc => hc.HabitId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique index on (HabitId, Date)
            modelBuilder.Entity<HabitCompletion>()
                .HasIndex(hc => new { hc.HabitId, hc.Date })
                .IsUnique();
        }
    }
}
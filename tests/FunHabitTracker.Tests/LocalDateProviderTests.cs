using FunHabitTracker.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunHabitTracker.Tests;

[TestClass]
public sealed class LocalDateProviderTests
{
    [TestMethod]
    public void Today_ChangesAtLocalMidnight()
    {
        var clock = new TestClock(new DateTime(2026, 8, 23, 21, 59, 59));
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(2), "Test", "Test");
        var provider = new LocalDateProvider(clock, zone);
        Assert.AreEqual(new DateOnly(2026, 8, 23), provider.Today);
        clock.UtcNow = new DateTime(2026, 8, 23, 22, 0, 0, DateTimeKind.Utc);
        Assert.AreEqual(new DateOnly(2026, 8, 24), provider.Today);
    }

    [TestMethod]
    public void Today_RemainsCalendarCorrectAcrossDaylightSavingTransition()
    {
        var adjustments = new[]
        {
            TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                new DateTime(2026, 1, 1),
                new DateTime(2026, 12, 31),
                TimeSpan.FromHours(1),
                TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 5, DayOfWeek.Sunday),
                TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 10, 5, DayOfWeek.Sunday))
        };
        var zone = TimeZoneInfo.CreateCustomTimeZone("DST-Test", TimeSpan.FromHours(1), "DST-Test", "DST-Test", "DST-Test", adjustments);
        var clock = new TestClock(new DateTime(2026, 3, 29, 0, 30, 0));
        var provider = new LocalDateProvider(clock, zone);
        Assert.AreEqual(new DateOnly(2026, 3, 29), provider.Today);
        clock.UtcNow = new DateTime(2026, 3, 29, 23, 0, 0, DateTimeKind.Utc);
        Assert.AreEqual(new DateOnly(2026, 3, 30), provider.Today);
    }
}

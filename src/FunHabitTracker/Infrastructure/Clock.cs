namespace FunHabitTracker.Infrastructure;

public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public interface ILocalDateProvider
{
    DateOnly Today { get; }
}

public sealed class LocalDateProvider : ILocalDateProvider
{
    private readonly IClock _clock;
    private readonly TimeZoneInfo _timeZone;

    public LocalDateProvider(IClock clock)
        : this(clock, TimeZoneInfo.Local)
    {
    }

    internal LocalDateProvider(IClock clock, TimeZoneInfo timeZone)
    {
        _clock = clock;
        _timeZone = timeZone;
    }

    public DateOnly Today => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(_clock.UtcNow, DateTimeKind.Utc), _timeZone));
}

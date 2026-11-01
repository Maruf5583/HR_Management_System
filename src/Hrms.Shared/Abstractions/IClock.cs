namespace Hrms.Shared.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
    DateOnly TodayLocal { get; }
    DateTime ToLocal(DateTime utc);
    DateTime ToUtc(DateTime local);
}

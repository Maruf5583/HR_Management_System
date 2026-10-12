using Hrms.Shared.Abstractions;

namespace Hrms.Web.Infrastructure;

public class SystemClock : IClock
{
    private static readonly TimeZoneInfo LocalTimeZone = GetTimeZone();

    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly TodayLocal => DateOnly.FromDateTime(ToLocal(UtcNow));

    public DateTime ToLocal(DateTime utc)
    {
        var unspecifiedUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(unspecifiedUtc, LocalTimeZone);
    }

    private static TimeZoneInfo GetTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Dhaka");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Local;
            }
        }
    }
}

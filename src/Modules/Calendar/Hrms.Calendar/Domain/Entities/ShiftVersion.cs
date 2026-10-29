namespace Hrms.Calendar.Domain.Entities;

public class ShiftVersion
{
    public Guid ShiftVersionId { get; set; }
    public Guid ShiftId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int GraceMinutes { get; set; }
    public int BreakMinutes { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Shift Shift { get; set; } = null!;

    public bool IsNightShift => EndTime < StartTime;

    public TimeSpan TotalShiftSpan => IsNightShift
        ? (TimeSpan.FromHours(24) - StartTime) + EndTime
        : EndTime - StartTime;

    public TimeSpan NetWorkingSpan => TotalShiftSpan - TimeSpan.FromMinutes(BreakMinutes);
}

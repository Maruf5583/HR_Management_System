namespace Hrms.Attendance.Domain.Entities;

public class AttendanceBreak
{
    public Guid AttendanceBreakId { get; set; } = Guid.NewGuid();
    public Guid AttendanceDayId { get; set; }
    public DateTime BreakStartUtc { get; set; }
    public DateTime? BreakEndUtc { get; set; }
    public int Minutes { get; set; }
}

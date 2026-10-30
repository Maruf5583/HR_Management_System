using Hrms.Attendance.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Attendance.Domain.Entities;

public class AttendanceDay : AuditableEntity
{
    public Guid AttendanceDayId { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public DateOnly WorkDate { get; set; }
    public Guid? ShiftVersionId { get; set; }
    public DateTime? CheckInUtc { get; set; }
    public DateTime? CheckOutUtc { get; set; }
    public int LateMinutes { get; set; }
    public int WorkedMinutes { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Note { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

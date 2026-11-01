using Hrms.Attendance.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Attendance.Domain.Entities;

public class AttendanceRequest : AuditableEntity
{
    public Guid AttendanceRequestId { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTime RequestedCheckInUtc { get; set; }
    public DateTime? RequestedCheckOutUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public AttendanceRequestStatus Status { get; set; } = AttendanceRequestStatus.Pending;
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewRemarks { get; set; }
}

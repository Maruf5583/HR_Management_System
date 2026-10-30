using Hrms.Leave.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Leave.Domain.Entities;

public class LeaveRecord : AuditableEntity
{
    public Guid LeaveRecordId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsHalfDay { get; set; }
    public decimal TotalDays { get; set; }
    public string? Reason { get; set; }
    public string? AttachmentKey { get; set; }
    public LeaveRecordStatus Status { get; set; } = LeaveRecordStatus.Approved;

    // Review fields
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewNote { get; set; }

    // Intra-module navigation
    public LeaveType? LeaveType { get; set; }

    public static LeaveRecord CreateApprovedDirect(
        Guid employeeId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        bool isHalfDay,
        decimal totalDays,
        string? reason,
        Guid? currentUserId,
        DateTime nowUtc)
    {
        return new LeaveRecord
        {
            LeaveRecordId = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            StartDate = startDate,
            EndDate = endDate,
            IsHalfDay = isHalfDay,
            TotalDays = totalDays,
            Reason = reason?.Trim(),
            Status = LeaveRecordStatus.Approved,
            ReviewedByUserId = currentUserId,
            ReviewedAtUtc = nowUtc,
            ReviewNote = "Directly recorded by HR/Admin",
            CreatedAtUtc = nowUtc,
            CreatedByUserId = currentUserId,
            UpdatedAtUtc = nowUtc,
            UpdatedByUserId = currentUserId
        };
    }
}

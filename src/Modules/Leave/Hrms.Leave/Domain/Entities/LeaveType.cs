using Hrms.Leave.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Leave.Domain.Entities;

public class LeaveType : AuditableEntity
{
    public Guid LeaveTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public decimal DaysPerYear { get; set; }
    public bool RequiresDocument { get; set; }
    public LeaveTypeStatus Status { get; set; } = LeaveTypeStatus.Active;

    public static LeaveType Create(
        string name,
        string code,
        bool isPaid,
        decimal daysPerYear,
        bool requiresDocument,
        Guid? createdByUserId = null)
    {
        return new LeaveType
        {
            LeaveTypeId = Guid.NewGuid(),
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            IsPaid = isPaid,
            DaysPerYear = daysPerYear,
            RequiresDocument = requiresDocument,
            Status = LeaveTypeStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };
    }
}

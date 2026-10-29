namespace Hrms.Calendar.Domain.Entities;

public class ShiftAssignment
{
    public Guid ShiftAssignmentId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ShiftId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public Guid? AssignedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Shift Shift { get; set; } = null!;

    public bool IsActive(DateOnly date) =>
        date >= EffectiveFrom && (EffectiveTo == null || date <= EffectiveTo.Value);
}

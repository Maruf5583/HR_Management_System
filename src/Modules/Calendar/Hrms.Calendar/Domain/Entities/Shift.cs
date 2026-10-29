using Hrms.Calendar.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Calendar.Domain.Entities;

public class Shift : AuditableEntity, IAuditableEntity
{
    public Guid ShiftId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ShiftStatus Status { get; set; } = ShiftStatus.Active;

    public ICollection<ShiftVersion> Versions { get; set; } = new List<ShiftVersion>();
    public ICollection<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();

    public ShiftVersion? CurrentVersion => Versions.FirstOrDefault(v => v.EffectiveTo == null);
}

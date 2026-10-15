using Hrms.Employees.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Employees.Domain.Entities;

public class Department : AuditableEntity, IAuditableEntity
{
    public Guid DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public ICollection<Designation> Designations { get; set; } = new List<Designation>();

    public static Department Create(string name) => new()
    {
        DepartmentId = Guid.NewGuid(),
        Name = name,
        Status = EntityStatus.Active
    };

    public void Update(string name)
    {
        Name = name;
    }

    public void Activate() => Status = EntityStatus.Active;
    public void Deactivate() => Status = EntityStatus.Inactive;
}

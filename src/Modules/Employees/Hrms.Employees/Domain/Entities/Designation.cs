using Hrms.Employees.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Employees.Domain.Entities;

public class Designation : AuditableEntity, IAuditableEntity
{
    public Guid DesignationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    // Navigation properties within module
    public Department? Department { get; set; }
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public static Designation Create(string title, Guid departmentId) => new()
    {
        DesignationId = Guid.NewGuid(),
        Title = title,
        DepartmentId = departmentId,
        Status = EntityStatus.Active
    };

    public void Update(string title, Guid departmentId)
    {
        Title = title;
        DepartmentId = departmentId;
    }

    public void Activate() => Status = EntityStatus.Active;
    public void Deactivate() => Status = EntityStatus.Inactive;
}

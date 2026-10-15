using Hrms.Employees.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Employees.Domain.Entities;

public class Grade : AuditableEntity, IAuditableEntity
{
    public Guid GradeId { get; set; }
    public Guid DesignationId { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    // Navigation property within module
    public Designation? Designation { get; set; }

    public static Grade Create(Guid designationId, string level, string code, decimal salary) => new()
    {
        GradeId = Guid.NewGuid(),
        DesignationId = designationId,
        Level = level,
        Code = code,
        Salary = salary,
        Status = EntityStatus.Active
    };

    public void Update(Guid designationId, string level, string code, decimal salary)
    {
        DesignationId = designationId;
        Level = level;
        Code = code;
        Salary = salary;
    }

    public void Activate() => Status = EntityStatus.Active;
    public void Deactivate() => Status = EntityStatus.Inactive;
}

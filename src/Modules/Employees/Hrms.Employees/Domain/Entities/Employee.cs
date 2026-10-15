using Hrms.Employees.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Employees.Domain.Entities;

public class Employee : AuditableEntity, IAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? NidNumberEncrypted { get; set; }
    public string? PhotoKey { get; set; }
    public DateOnly JoinDate { get; set; }
    public EmploymentType EmploymentType { get; set; } = EmploymentType.Permanent;
    public Guid DepartmentId { get; set; }
    public Guid DesignationId { get; set; }
    public Guid GradeId { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public DateOnly? ExitDate { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Designation? Designation { get; set; }
    public Grade? Grade { get; set; }
    public Department? Department => Designation?.Department;

    public string FullName => $"{FirstName} {LastName}".Trim();
}

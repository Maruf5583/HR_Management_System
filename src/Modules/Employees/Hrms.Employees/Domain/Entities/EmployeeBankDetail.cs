using Hrms.Employees.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Employees.Domain.Entities;

public class EmployeeBankDetail : AuditableEntity, IAuditableEntity
{
    public Guid EmployeeBankDetailId { get; set; }
    public Guid EmployeeId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string? RoutingNumber { get; set; }
    public string? SwiftCode { get; set; }
    public BankDetailStatus Status { get; set; } = BankDetailStatus.Pending;
    public bool IsActive { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewRemarks { get; set; }

    public Employee? Employee { get; set; }
}

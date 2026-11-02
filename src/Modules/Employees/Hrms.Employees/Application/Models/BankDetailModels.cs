using Hrms.Employees.Domain.Enums;

namespace Hrms.Employees.Application.Models;

public record BankDetailDto(
    Guid EmployeeBankDetailId,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string DepartmentName,
    string DesignationTitle,
    string BankName,
    string AccountHolderName,
    string AccountNumber,
    string BranchName,
    string? RoutingNumber,
    string? SwiftCode,
    BankDetailStatus Status,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? ReviewedAtUtc,
    string? ReviewRemarks
);

public record EmployeeBankProfileDto(
    BankDetailDto? ActiveBankDetail,
    BankDetailDto? PendingRequest,
    BankDetailDto? LastRejectedRequest
);

public record SubmitBankDetailModel(
    Guid EmployeeId,
    string BankName,
    string AccountHolderName,
    string AccountNumber,
    string BranchName,
    string? RoutingNumber,
    string? SwiftCode
);

public record ReviewBankDetailModel(
    Guid EmployeeBankDetailId,
    bool IsApproved,
    string? ReviewRemarks
);

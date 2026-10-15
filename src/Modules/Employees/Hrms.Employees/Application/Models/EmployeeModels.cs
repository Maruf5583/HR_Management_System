using Hrms.Employees.Domain.Enums;

namespace Hrms.Employees.Application.Models;

public record EmployeeFilterDto(
    string? Search = null,
    Guid? DepartmentId = null,
    Guid? DesignationId = null,
    EmployeeStatus? Status = null,
    EmploymentType? EmploymentType = null,
    int PageNumber = 1,
    int PageSize = 10
);

public record EmployeeListItemDto(
    Guid EmployeeId,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string FullName,
    string? Email,
    string? Phone,
    string DepartmentName,
    string DesignationTitle,
    string GradeCode,
    EmploymentType EmploymentType,
    EmployeeStatus Status,
    DateOnly JoinDate,
    string? PhotoKey
);

public record EmployeeDetailsDto(
    Guid EmployeeId,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string FullName,
    string? Email,
    string? Phone,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Address,
    string? NidNumber,
    string? PhotoKey,
    DateOnly JoinDate,
    EmploymentType EmploymentType,
    Guid DepartmentId,
    string DepartmentName,
    Guid DesignationId,
    string DesignationTitle,
    Guid GradeId,
    string GradeLevel,
    string GradeCode,
    decimal Salary,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    EmployeeStatus Status,
    DateOnly? ExitDate,
    byte[] RowVersion,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public record CreateEmployeeModel(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Address,
    string? NidNumber,
    DateOnly JoinDate,
    EmploymentType EmploymentType,
    Guid DepartmentId,
    Guid DesignationId,
    Guid GradeId,
    string? EmergencyContactName,
    string? EmergencyContactPhone
);

public record UpdateEmployeeModel(
    Guid EmployeeId,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Address,
    string? NidNumber,
    DateOnly JoinDate,
    EmploymentType EmploymentType,
    Guid DepartmentId,
    Guid DesignationId,
    Guid GradeId,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    byte[] RowVersion
);

public record ChangeEmployeeStatusModel(
    Guid EmployeeId,
    EmployeeStatus Status,
    DateOnly? ExitDate,
    byte[] RowVersion
);

public record UploadPhotoModel(
    Guid EmployeeId,
    string FileName,
    byte[] Content
);

public record LookupItemDto(
    Guid Id,
    string Name
);

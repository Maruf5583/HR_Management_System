using Hrms.Employees.Domain.Enums;

namespace Hrms.Employees.Application.Models;

public record DepartmentDto(
    Guid Id,
    string Name,
    EntityStatus Status,
    int DesignationCount,
    int EmployeeCount,
    DateTime CreatedAtUtc
);

public record CreateDepartmentModel(
    string Name
);

public record UpdateDepartmentModel(
    Guid Id,
    string Name
);

public record DesignationDto(
    Guid Id,
    string Title,
    Guid DepartmentId,
    string DepartmentName,
    EntityStatus Status,
    int GradeCount,
    int EmployeeCount,
    DateTime CreatedAtUtc
);

public record CreateDesignationModel(
    string Title,
    Guid DepartmentId
);

public record UpdateDesignationModel(
    Guid Id,
    string Title,
    Guid DepartmentId
);

public record GradeDto(
    Guid Id,
    Guid DesignationId,
    string DesignationTitle,
    string DepartmentName,
    string Level,
    string Code,
    decimal Salary,
    EntityStatus Status,
    int EmployeeCount,
    DateTime CreatedAtUtc
);

public record CreateGradeModel(
    Guid DesignationId,
    string Level,
    string Code,
    decimal Salary
);

public record UpdateGradeModel(
    Guid Id,
    Guid DesignationId,
    string Level,
    string Code,
    decimal Salary
);

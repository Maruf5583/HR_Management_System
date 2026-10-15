using Hrms.Identity.Domain.Enums;

namespace Hrms.Identity.Application.Models;

public record UserDto(
    Guid Id,
    string Email,
    Guid? EmployeeId,
    UserStatus Status,
    DateTime? LastLoginAtUtc,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc
);

public record CreateUserModel(
    string Email,
    string Password,
    string RoleName,
    Guid? EmployeeId = null
);

public record UpdateUserModel(
    Guid Id,
    string Email,
    string RoleName,
    Guid? EmployeeId = null
);

public record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    int UserCount,
    IReadOnlyList<string> Permissions
);

public record CreateRoleModel(
    string Name,
    string? Description,
    IEnumerable<string>? Permissions = null
);

public record UpdateRoleModel(
    Guid Id,
    string? Description,
    IEnumerable<string> Permissions
);

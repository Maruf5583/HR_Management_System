using Microsoft.AspNetCore.Identity;
using Hrms.Shared.Primitives;

namespace Hrms.Identity.Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>, IAuditableEntity
{
    public string? Description { get; set; }
    public bool IsSystem { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public const string AdminRoleName = "Admin";
    public const string HrRoleName = "HR";
    public const string EmployeeRoleName = "Employee";

    public static readonly IReadOnlyList<string> SystemRoles = new[]
    {
        AdminRoleName,
        HrRoleName,
        EmployeeRoleName
    };

    public static bool IsSystemRole(string roleName)
    {
        return SystemRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
    }
}

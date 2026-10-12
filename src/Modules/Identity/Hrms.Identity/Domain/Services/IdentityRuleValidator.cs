using Hrms.Identity.Domain.Entities;
using Hrms.Identity.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Identity.Domain.Services;

public static class IdentityRuleValidator
{
    public static Result ValidateRoleDeletion(ApplicationRole role, int assignedUserCount)
    {
        if (role.IsSystem || ApplicationRole.IsSystemRole(role.Name ?? string.Empty))
        {
            return Result.Failure("System roles cannot be deleted.");
        }

        if (assignedUserCount > 0)
        {
            return Result.Failure("A role cannot be deleted while users hold it.");
        }

        return Result.Success();
    }

    public static Result ValidateRoleUpdate(ApplicationRole role, string newName)
    {
        if (role.IsSystem || ApplicationRole.IsSystemRole(role.Name ?? string.Empty))
        {
            if (!string.Equals(role.Name, newName, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure("System roles cannot be renamed.");
            }
        }

        return Result.Success();
    }

    public static Result ValidateRolePermissionModification(ApplicationRole role)
    {
        if (string.Equals(role.Name, ApplicationRole.AdminRoleName, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure("Admin role permissions are locked and cannot be modified.");
        }

        return Result.Success();
    }

    public static Result ValidateUserDeactivation(bool isUserAdmin, int activeAdminCount)
    {
        if (isUserAdmin && activeAdminCount <= 1)
        {
            return Result.Failure("The last active Admin cannot be deactivated.");
        }

        return Result.Success();
    }

    public static Result ValidateUserDemotion(bool isUserCurrentlyAdmin, bool willBeAdmin, int activeAdminCount)
    {
        if (isUserCurrentlyAdmin && !willBeAdmin && activeAdminCount <= 1)
        {
            return Result.Failure("The last active Admin cannot be demoted.");
        }

        return Result.Success();
    }

    public static Result ValidateUserDeletion(bool isUserAdmin, int activeAdminCount)
    {
        if (isUserAdmin && activeAdminCount <= 1)
        {
            return Result.Failure("The last active Admin cannot be deleted.");
        }

        return Result.Success();
    }

    public static Result ValidateUserSignIn(ApplicationUser user)
    {
        if (user.Status != UserStatus.Active)
        {
            return Result.Failure("Inactive users cannot sign in.");
        }

        return Result.Success();
    }
}

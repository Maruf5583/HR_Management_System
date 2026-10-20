using System.Security.Claims;
using System.Text.Json;
using Hrms.Audit.Contracts;
using Hrms.Audit.Contracts.Models;
using Hrms.Identity.Application.Models;
using Hrms.Identity.Application.Services;
using Hrms.Identity.Domain.Entities;
using Hrms.Identity.Domain.Services;
using Hrms.Shared.Primitives;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Identity.Infrastructure.Services;

public class RoleService : IRoleService
{
    private const string PermissionClaimType = "permission";

    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditWriter? _auditWriter;

    public RoleService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IAuditWriter? auditWriter = null)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _auditWriter = auditWriter;
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleManager.Roles
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var result = new List<RoleDto>(roles.Count);
        foreach (var role in roles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            var claims = await _roleManager.GetClaimsAsync(role);
            var permissions = claims
                .Where(c => c.Type == PermissionClaimType)
                .Select(c => c.Value)
                .ToList();

            result.Add(new RoleDto(
                role.Id,
                role.Name ?? string.Empty,
                role.Description,
                role.IsSystem,
                usersInRole.Count,
                permissions
            ));
        }

        return result;
    }

    public async Task<RoleDto?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
        {
            return null;
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        var claims = await _roleManager.GetClaimsAsync(role);
        var permissions = claims
            .Where(c => c.Type == PermissionClaimType)
            .Select(c => c.Value)
            .ToList();

        return new RoleDto(
            role.Id,
            role.Name ?? string.Empty,
            role.Description,
            role.IsSystem,
            usersInRole.Count,
            permissions
        );
    }

    public async Task<Result<Guid>> CreateRoleAsync(CreateRoleModel model, CancellationToken cancellationToken = default)
    {
        if (ApplicationRole.IsSystemRole(model.Name))
        {
            return Result<Guid>.Failure("Cannot create a role with a reserved system name.");
        }

        var exists = await _roleManager.RoleExistsAsync(model.Name);
        if (exists)
        {
            return Result<Guid>.Failure($"Role '{model.Name}' already exists.");
        }

        var role = new ApplicationRole
        {
            Name = model.Name,
            Description = model.Description,
            IsSystem = false
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result<Guid>.Failure(errors);
        }

        if (model.Permissions != null)
        {
            foreach (var permission in model.Permissions.Distinct())
            {
                await _roleManager.AddClaimAsync(role, new Claim(PermissionClaimType, permission));
            }
        }

        if (_auditWriter != null)
        {
            var newValues = new
            {
                role.Name,
                role.Description,
                Permissions = model.Permissions?.Distinct().ToList()
            };

            await _auditWriter.WriteAsync(new AuditLogEntry(
                Action: "CREATE",
                EntityName: "Role",
                EntityId: role.Id.ToString(),
                OldValues: null,
                NewValues: JsonSerializer.Serialize(newValues)
            ), cancellationToken);
        }

        return Result<Guid>.Success(role.Id);
    }

    public async Task<Result> UpdateRoleAsync(UpdateRoleModel model, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(model.Id.ToString());
        if (role == null)
        {
            return Result.Failure("Role not found.");
        }

        var permissionModValidation = IdentityRuleValidator.ValidateRolePermissionModification(role);
        if (permissionModValidation.IsFailure)
        {
            return permissionModValidation;
        }

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        var currentPermissionClaims = existingClaims
            .Where(c => c.Type == PermissionClaimType)
            .ToList();

        var oldValues = new
        {
            role.Name,
            Description = role.Description,
            Permissions = currentPermissionClaims.Select(c => c.Value).ToList()
        };

        role.Description = model.Description;
        var updateResult = await _roleManager.UpdateAsync(role);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        var newPermissions = model.Permissions.Distinct().ToHashSet();

        // Remove unselected claims
        foreach (var claim in currentPermissionClaims)
        {
            if (!newPermissions.Contains(claim.Value))
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }
        }

        // Add newly selected claims
        var currentSet = currentPermissionClaims.Select(c => c.Value).ToHashSet();
        foreach (var permission in newPermissions)
        {
            if (!currentSet.Contains(permission))
            {
                await _roleManager.AddClaimAsync(role, new Claim(PermissionClaimType, permission));
            }
        }

        if (_auditWriter != null)
        {
            var newValues = new
            {
                role.Name,
                Description = model.Description,
                Permissions = model.Permissions.Distinct().ToList()
            };

            await _auditWriter.WriteAsync(new AuditLogEntry(
                Action: "UPDATE",
                EntityName: "Role",
                EntityId: role.Id.ToString(),
                OldValues: JsonSerializer.Serialize(oldValues),
                NewValues: JsonSerializer.Serialize(newValues)
            ), cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
        {
            return Result.Failure("Role not found.");
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        var validation = IdentityRuleValidator.ValidateRoleDeletion(role, usersInRole.Count);
        if (validation.IsFailure)
        {
            return validation;
        }

        var oldValues = new
        {
            role.Name,
            role.Description
        };

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        if (_auditWriter != null)
        {
            await _auditWriter.WriteAsync(new AuditLogEntry(
                Action: "DELETE",
                EntityName: "Role",
                EntityId: role.Id.ToString(),
                OldValues: JsonSerializer.Serialize(oldValues),
                NewValues: null
            ), cancellationToken);
        }

        return Result.Success();
    }
}

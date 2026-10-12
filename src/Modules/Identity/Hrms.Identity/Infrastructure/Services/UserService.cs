using Hrms.Identity.Application.Models;
using Hrms.Identity.Application.Services;
using Hrms.Identity.Domain.Entities;
using Hrms.Identity.Domain.Enums;
using Hrms.Identity.Domain.Services;
using Hrms.Shared.Primitives;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Identity.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var userDtos = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto(
                user.Id,
                user.Email ?? string.Empty,
                user.EmployeeId,
                user.Status,
                user.LastLoginAtUtc,
                roles.ToList(),
                user.CreatedAtUtc
            ));
        }

        return userDtos;
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.EmployeeId,
            user.Status,
            user.LastLoginAtUtc,
            roles.ToList(),
            user.CreatedAtUtc
        );
    }

    public async Task<Result<Guid>> CreateUserAsync(CreateUserModel model, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            return Result<Guid>.Failure("An account with this email address already exists.");
        }

        var roleExists = await _roleManager.RoleExistsAsync(model.RoleName);
        if (!roleExists)
        {
            return Result<Guid>.Failure($"Role '{model.RoleName}' does not exist.");
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            Status = UserStatus.Active,
            EmployeeId = model.EmployeeId,
            LockoutEnabled = true,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return Result<Guid>.Failure(errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, model.RoleName);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            return Result<Guid>.Failure($"User created but role assignment failed: {errors}");
        }

        return Result<Guid>.Success(user.Id);
    }

    public async Task<Result> UpdateUserAsync(UpdateUserModel model, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(model.Id.ToString());
        if (user == null)
        {
            return Result.Failure("User not found.");
        }

        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingByEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingByEmail != null && existingByEmail.Id != user.Id)
            {
                return Result.Failure("An account with this email address already exists.");
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        bool isCurrentlyAdmin = currentRoles.Contains(ApplicationRole.AdminRoleName, StringComparer.OrdinalIgnoreCase);
        bool willBeAdmin = string.Equals(model.RoleName, ApplicationRole.AdminRoleName, StringComparison.OrdinalIgnoreCase);

        if (isCurrentlyAdmin && !willBeAdmin)
        {
            var activeAdminCount = await GetActiveAdminCountAsync();
            var demotionValidation = IdentityRuleValidator.ValidateUserDemotion(true, false, activeAdminCount);
            if (demotionValidation.IsFailure)
            {
                return demotionValidation;
            }
        }

        user.Email = model.Email;
        user.UserName = model.Email;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        if (!currentRoles.Contains(model.RoleName, StringComparer.OrdinalIgnoreCase))
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                var errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                return Result.Failure(errors);
            }

            var addResult = await _userManager.AddToRoleAsync(user, model.RoleName);
            if (!addResult.Succeeded)
            {
                var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
                return Result.Failure(errors);
            }
        }

        return Result.Success();
    }

    public async Task<Result> SetUserStatusAsync(Guid id, UserStatus newStatus, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return Result.Failure("User not found.");
        }

        if (user.Status == newStatus)
        {
            return Result.Success();
        }

        if (newStatus == UserStatus.Inactive)
        {
            var isUserAdmin = await _userManager.IsInRoleAsync(user, ApplicationRole.AdminRoleName);
            if (isUserAdmin)
            {
                var activeAdminCount = await GetActiveAdminCountAsync();
                var deactivationValidation = IdentityRuleValidator.ValidateUserDeactivation(true, activeAdminCount);
                if (deactivationValidation.IsFailure)
                {
                    return deactivationValidation;
                }
            }
        }

        user.Status = newStatus;

        if (newStatus == UserStatus.Inactive)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return Result.Failure("User not found.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await _userManager.UpdateSecurityStampAsync(user);
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(Guid id, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return Result.Failure("User not found.");
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await _userManager.UpdateSecurityStampAsync(user);
        return Result.Success();
    }

    private async Task<int> GetActiveAdminCountAsync()
    {
        var adminUsers = await _userManager.GetUsersInRoleAsync(ApplicationRole.AdminRoleName);
        return adminUsers.Count(u => u.Status == UserStatus.Active);
    }
}

using System.Security.Claims;
using Hrms.Identity.Domain.Entities;
using Hrms.Identity.Domain.Enums;
using Hrms.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hrms.Persistence.Seed;

public class IdentitySeeder
{
    private const string PermissionClaimType = "permission";

    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<IdentitySeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedUsersAsync();
    }

    private async Task SeedRolesAsync()
    {
        var systemRoles = new[]
        {
            (Name: ApplicationRole.AdminRoleName, Description: "System Administrator", Permissions: Permissions.AdminDefault),
            (Name: ApplicationRole.HrRoleName, Description: "Human Resources Manager", Permissions: Permissions.HrDefault),
            (Name: ApplicationRole.EmployeeRoleName, Description: "Company Employee", Permissions: Permissions.EmployeeDefault)
        };

        foreach (var (name, description, permissions) in systemRoles)
        {
            var role = await _roleManager.FindByNameAsync(name);
            if (role == null)
            {
                role = new ApplicationRole
                {
                    Name = name,
                    Description = description,
                    IsSystem = true
                };

                var createResult = await _roleManager.CreateAsync(role);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("Failed to create role {RoleName}: {Errors}",
                        name, string.Join("; ", createResult.Errors.Select(e => e.Description)));
                    continue;
                }
            }
            else
            {
                if (!role.IsSystem)
                {
                    role.IsSystem = true;
                    await _roleManager.UpdateAsync(role);
                }
            }

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var existingClaimValues = existingClaims
                .Where(c => c.Type == PermissionClaimType)
                .Select(c => c.Value)
                .ToHashSet();

            foreach (var permission in permissions)
            {
                if (!existingClaimValues.Contains(permission))
                {
                    await _roleManager.AddClaimAsync(role, new Claim(PermissionClaimType, permission));
                }
            }
        }
    }

    private async Task SeedUsersAsync()
    {
        var seedUsers = new[]
        {
            (
                Email: _configuration["Seed:AdminEmail"] ?? "admin@hrms.local",
                Password: _configuration["Seed:AdminPassword"] ?? "Admin@123456",
                Role: ApplicationRole.AdminRoleName
            ),
            (
                Email: _configuration["Seed:HrEmail"] ?? "hr@hrms.local",
                Password: _configuration["Seed:HrPassword"] ?? "Hr@123456",
                Role: ApplicationRole.HrRoleName
            ),
            (
                Email: _configuration["Seed:EmployeeEmail"] ?? "employee@hrms.local",
                Password: _configuration["Seed:EmployeePassword"] ?? "Employee@123456",
                Role: ApplicationRole.EmployeeRoleName
            )
        };

        foreach (var (email, password, role) in seedUsers)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                continue;
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                if (!await _userManager.IsInRoleAsync(existingUser, role))
                {
                    await _userManager.AddToRoleAsync(existingUser, role);
                }
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Status = UserStatus.Active,
                LockoutEnabled = true,
                EmployeeId = null
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                _logger.LogInformation("Successfully created and seeded user: {Email} ({Role})", email, role);
            }
            else
            {
                _logger.LogError("Failed to create seed user {Email}: {Errors}",
                    email, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}

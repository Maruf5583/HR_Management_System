using System.Security.Claims;
using Hrms.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Hrms.Web.Infrastructure;

public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    private const string PermissionClaimType = "permission";

    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.EmployeeId.HasValue)
        {
            identity.AddClaim(new Claim("employee_id", user.EmployeeId.Value.ToString()));
        }

        var roles = await UserManager.GetRolesAsync(user);
        var permissionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in roles)
        {
            var role = await RoleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var roleClaims = await RoleManager.GetClaimsAsync(role);
                foreach (var claim in roleClaims.Where(c => c.Type == PermissionClaimType))
                {
                    permissionSet.Add(claim.Value);
                }
            }
        }

        foreach (var permission in permissionSet)
        {
            identity.AddClaim(new Claim(PermissionClaimType, permission));
        }

        return identity;
    }
}

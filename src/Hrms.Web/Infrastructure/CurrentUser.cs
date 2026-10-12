using System.Security.Claims;
using Hrms.Shared.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Hrms.Web.Infrastructure;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public Guid? EmployeeId
    {
        get
        {
            var empClaim = User?.FindFirst("employee_id")?.Value;
            return Guid.TryParse(empClaim, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool HasPermission(string permission)
    {
        if (User == null) return false;
        return User.HasClaim("permission", permission);
    }

    public bool IsInRole(string role)
    {
        return User?.IsInRole(role) ?? false;
    }
}

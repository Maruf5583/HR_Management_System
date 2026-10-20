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

    public string? IpAddress
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                var clientIp = forwardedFor.Split(',')[0].Trim();
                if (!string.IsNullOrWhiteSpace(clientIp))
                {
                    return NormalizeIp(clientIp);
                }
            }

            var remoteIp = httpContext.Connection.RemoteIpAddress;
            return NormalizeIp(remoteIp?.ToString());
        }
    }

    private static string? NormalizeIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;
        if (ip == "::1" || ip == "0:0:0:0:0:0:0:1") return "127.0.0.1";
        if (ip.StartsWith("::ffff:")) return ip[7..];

        if (System.Net.IPAddress.TryParse(ip, out var parsed))
        {
            if (parsed.IsIPv4MappedToIPv6)
            {
                return parsed.MapToIPv4().ToString();
            }
            if (System.Net.IPAddress.IsLoopback(parsed))
            {
                return "127.0.0.1";
            }
        }

        return ip;
    }
}

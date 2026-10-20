namespace Hrms.Shared.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? EmployeeId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
    bool IsInRole(string role);
    string? IpAddress => null;
}

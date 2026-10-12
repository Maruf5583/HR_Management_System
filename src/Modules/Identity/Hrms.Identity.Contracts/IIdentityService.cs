namespace Hrms.Identity.Contracts;

public interface IIdentityService
{
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<string?> GetUserEmailAsync(Guid userId, CancellationToken cancellationToken = default);
}

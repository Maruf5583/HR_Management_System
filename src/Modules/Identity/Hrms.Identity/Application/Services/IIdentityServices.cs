using Hrms.Identity.Application.Models;
using Hrms.Identity.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Identity.Application.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateUserAsync(CreateUserModel model, CancellationToken cancellationToken = default);
    Task<Result> UpdateUserAsync(UpdateUserModel model, CancellationToken cancellationToken = default);
    Task<Result> SetUserStatusAsync(Guid id, UserStatus newStatus, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(Guid id, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<RoleDto?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateRoleAsync(CreateRoleModel model, CancellationToken cancellationToken = default);
    Task<Result> UpdateRoleAsync(UpdateRoleModel model, CancellationToken cancellationToken = default);
    Task<Result> DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default);
}

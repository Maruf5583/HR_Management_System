using Microsoft.AspNetCore.Identity;
using Hrms.Identity.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Identity.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>, IAuditableEntity
{
    public Guid? EmployeeId { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime? LastLoginAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}

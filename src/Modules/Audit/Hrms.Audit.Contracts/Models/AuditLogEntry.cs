namespace Hrms.Audit.Contracts.Models;

public sealed record AuditLogEntry(
    Guid? UserId = null,
    string Action = "",
    string EntityName = "",
    string? EntityId = null,
    string? OldValues = null,
    string? NewValues = null,
    string? IpAddress = null,
    DateTime? CreatedAtUtc = null);

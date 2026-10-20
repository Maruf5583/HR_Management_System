namespace Hrms.Audit.Contracts.Models;

public sealed record AuditLogDto(
    long AuditLogId,
    Guid? UserId,
    string? UserEmail,
    string Action,
    string EntityName,
    string? EntityId,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    DateTime CreatedAtUtc);

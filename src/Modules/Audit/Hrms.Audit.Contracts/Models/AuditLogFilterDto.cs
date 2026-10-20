namespace Hrms.Audit.Contracts.Models;

public sealed record AuditLogFilterDto(
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    string? EntityName = null,
    string? Action = null,
    Guid? UserId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 20);

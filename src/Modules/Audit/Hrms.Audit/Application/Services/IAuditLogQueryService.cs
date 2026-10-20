using Hrms.Audit.Contracts.Models;
using Hrms.Shared.Primitives;

namespace Hrms.Audit.Application.Services;

public interface IAuditLogQueryService
{
    Task<PagedResult<AuditLogDto>> GetPagedAsync(AuditLogFilterDto filter, CancellationToken cancellationToken = default);
    Task<AuditLogDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctEntityNamesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctActionsAsync(CancellationToken cancellationToken = default);
}

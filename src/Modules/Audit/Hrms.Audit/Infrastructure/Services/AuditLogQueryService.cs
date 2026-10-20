using Hrms.Audit.Application.Abstractions;
using Hrms.Audit.Application.Services;
using Hrms.Audit.Contracts.Models;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Audit.Infrastructure.Services;

public class AuditLogQueryService : IAuditLogQueryService
{
    private readonly IAuditDbContext _context;

    public AuditLogQueryService(IAuditDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AuditLogDto>> GetPagedAsync(AuditLogFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs
            .AsNoTracking();

        if (filter.DateFrom.HasValue)
        {
            var fromUtc = filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.CreatedAtUtc >= fromUtc);
        }

        if (filter.DateTo.HasValue)
        {
            var toUtc = filter.DateTo.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(a => a.CreatedAtUtc <= toUtc);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityName))
        {
            var entity = filter.EntityName.Trim();
            query = query.Where(a => a.EntityName == entity);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            var action = filter.Action.Trim();
            query = query.Where(a => a.Action == action);
        }

        if (filter.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == filter.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(a =>
                a.EntityName.Contains(term) ||
                (a.EntityId != null && a.EntityId.Contains(term)) ||
                a.Action.Contains(term) ||
                (a.IpAddress != null && a.IpAddress.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 20 : (filter.PageSize > 100 ? 100 : filter.PageSize);

        var items = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .ThenByDescending(a => a.AuditLogId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.AuditLogId,
                a.UserId,
                null,
                a.Action,
                a.EntityName,
                a.EntityId,
                a.OldValues,
                a.NewValues,
                a.IpAddress,
                a.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<AuditLogDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var log = await _context.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AuditLogId == id, cancellationToken);

        if (log == null) return null;

        return new AuditLogDto(
            log.AuditLogId,
            log.UserId,
            null,
            log.Action,
            log.EntityName,
            log.EntityId,
            log.OldValues,
            log.NewValues,
            log.IpAddress,
            log.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<string>> GetDistinctEntityNamesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Select(a => a.EntityName)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctActionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync(cancellationToken);
    }
}

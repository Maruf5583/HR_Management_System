using Hrms.Audit.Application.Abstractions;
using Hrms.Audit.Contracts;
using Hrms.Audit.Contracts.Models;
using Hrms.Audit.Domain.Entities;
using Hrms.Audit.Infrastructure.Common;
using Hrms.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Hrms.Audit.Infrastructure.Services;

public class AuditWriter : IAuditWriter
{
    private readonly IAuditDbContext _context;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AuditWriter> _logger;

    public AuditWriter(
        IAuditDbContext context,
        IClock clock,
        ICurrentUser currentUser,
        ILogger<AuditWriter>? logger = null)
    {
        _context = context;
        _clock = clock;
        _currentUser = currentUser;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditWriter>.Instance;
    }

    public async Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrWhiteSpace(entry.Action))
        {
            throw new ArgumentException("Action cannot be null or empty.", nameof(entry));
        }

        if (string.IsNullOrWhiteSpace(entry.EntityName))
        {
            throw new ArgumentException("EntityName cannot be null or empty.", nameof(entry));
        }

        var userId = entry.UserId ?? _currentUser.UserId;
        var createdAt = entry.CreatedAtUtc ?? _clock.UtcNow;

        var sanitizedOld = AuditDataSanitizer.SanitizeJson(entry.OldValues);
        var sanitizedNew = AuditDataSanitizer.SanitizeJson(entry.NewValues);

        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = entry.Action.Trim(),
            EntityName = entry.EntityName.Trim(),
            EntityId = entry.EntityId?.Trim(),
            OldValues = sanitizedOld,
            NewValues = sanitizedNew,
            IpAddress = entry.IpAddress?.Trim(),
            CreatedAtUtc = createdAt
        };

        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("AuditLog recorded: Action '{Action}' on '{EntityName}' (ID: {EntityId}) by User {UserId}",
            auditLog.Action, auditLog.EntityName, auditLog.EntityId, auditLog.UserId);
    }
}

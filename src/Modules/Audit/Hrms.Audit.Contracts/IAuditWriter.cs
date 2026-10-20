using Hrms.Audit.Contracts.Models;

namespace Hrms.Audit.Contracts;

public interface IAuditWriter
{
    Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}

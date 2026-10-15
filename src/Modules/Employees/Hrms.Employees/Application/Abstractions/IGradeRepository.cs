using Hrms.Employees.Domain.Entities;

namespace Hrms.Employees.Application.Abstractions;

public interface IGradeRepository
{
    Task<Grade?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByLevelAsync(Guid designationId, string level, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Grade grade, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

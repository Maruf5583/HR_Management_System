using Hrms.Employees.Domain.Entities;

namespace Hrms.Employees.Application.Abstractions;

public interface IDesignationRepository
{
    Task<Designation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByTitleAsync(Guid departmentId, string title, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Designation designation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Hrms.Calendar.Domain.Entities;
using Hrms.Calendar.Domain.Enums;

namespace Hrms.Calendar.Application.Abstractions;

public interface IShiftRepository
{
    Task<IReadOnlyList<Shift>> GetAllAsync(ShiftStatus? statusFilter = null, CancellationToken cancellationToken = default);
    Task<Shift?> GetByIdAsync(Guid shiftId, CancellationToken cancellationToken = default);
    Task<Shift?> GetByIdWithVersionsAsync(Guid shiftId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeShiftId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Shift shift, CancellationToken cancellationToken = default);
    Task AddVersionAsync(ShiftVersion version, CancellationToken cancellationToken = default);
    Task<ShiftVersion?> GetLatestVersionAsync(Guid shiftId, CancellationToken cancellationToken = default);
    Task<ShiftVersion?> GetVersionForDateAsync(Guid shiftId, DateOnly date, CancellationToken cancellationToken = default);
    void Delete(Shift shift);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Hrms.Calendar.Domain.Entities;

namespace Hrms.Calendar.Application.Abstractions;

public interface IShiftAssignmentRepository
{
    Task<IReadOnlyList<ShiftAssignment>> GetAssignmentsForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShiftAssignment>> GetAssignmentsForShiftAsync(Guid shiftId, CancellationToken cancellationToken = default);
    Task<ShiftAssignment?> GetCurrentAssignmentAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<ShiftAssignment?> GetAssignmentForDateAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    Task<ShiftAssignment?> GetByIdAsync(Guid shiftAssignmentId, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingAssignmentAsync(Guid employeeId, DateOnly effectiveFrom, DateOnly? effectiveTo, Guid? excludeAssignmentId = null, CancellationToken cancellationToken = default);
    Task AddAsync(ShiftAssignment assignment, CancellationToken cancellationToken = default);
    void Delete(ShiftAssignment assignment);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

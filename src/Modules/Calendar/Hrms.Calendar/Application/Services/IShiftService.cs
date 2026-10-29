using Hrms.Calendar.Application.Models;
using Hrms.Calendar.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Calendar.Application.Services;

public interface IShiftService
{
    Task<IReadOnlyList<ShiftDto>> GetShiftsAsync(ShiftStatus? status = null, CancellationToken cancellationToken = default);
    Task<ShiftDetailsDto?> GetShiftDetailsAsync(Guid shiftId, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateShiftAsync(CreateShiftCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateShiftNameAsync(Guid shiftId, string newName, CancellationToken cancellationToken = default);
    Task<Result> DeleteShiftAsync(Guid shiftId, CancellationToken cancellationToken = default);
    Task<Result> CreateShiftVersionAsync(CreateShiftVersionCommand command, CancellationToken cancellationToken = default);
    Task<Result> SetShiftStatusAsync(Guid shiftId, ShiftStatus status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShiftAssignmentDto>> GetAssignmentsAsync(Guid? shiftId = null, Guid? employeeId = null, CancellationToken cancellationToken = default);
    Task<ShiftAssignmentDto?> GetAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default);
    Task<Result<Guid>> AssignShiftAsync(AssignShiftCommand command, CancellationToken cancellationToken = default);
    Task<Result<Guid>> TransferShiftAsync(TransferShiftCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateAssignmentAsync(Guid assignmentId, DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken = default);
    Task<Result> DeleteAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default);
    Task<Result> EndAssignmentAsync(EndShiftAssignmentCommand command, CancellationToken cancellationToken = default);
}

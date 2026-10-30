using Hrms.Attendance.Contracts;
using Hrms.Calendar.Contracts;
using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Application.Services;
using Hrms.Leave.Domain.Enums;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Application.Commands.Approvals;

public record ApproveLeaveRequestCommand(Guid LeaveRecordId);

public interface IApproveLeaveRequestHandler
{
    Task<Result> HandleAsync(ApproveLeaveRequestCommand command, CancellationToken cancellationToken = default);
}

public class ApproveLeaveRequestHandler : IApproveLeaveRequestHandler
{
    private readonly ILeaveDbContext _context;
    private readonly ILeaveRecordRepository _recordRepository;
    private readonly ILeaveBalanceRepository _balanceRepository;
    private readonly ILeaveTypeRepository _typeRepository;
    private readonly ILeaveYearHelper _yearHelper;
    private readonly ILeaveDaysCalculator _daysCalculator;
    private readonly IAttendanceMarker _attendanceMarker;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public ApproveLeaveRequestHandler(
        ILeaveDbContext context,
        ILeaveRecordRepository recordRepository,
        ILeaveBalanceRepository balanceRepository,
        ILeaveTypeRepository typeRepository,
        ILeaveYearHelper yearHelper,
        ILeaveDaysCalculator daysCalculator,
        IAttendanceMarker attendanceMarker,
        ICurrentUser currentUser,
        IClock clock)
    {
        _context = context;
        _recordRepository = recordRepository;
        _balanceRepository = balanceRepository;
        _typeRepository = typeRepository;
        _yearHelper = yearHelper;
        _daysCalculator = daysCalculator;
        _attendanceMarker = attendanceMarker;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(ApproveLeaveRequestCommand command, CancellationToken cancellationToken = default)
    {
        var dbContext = (DbContext)_context;
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. Lock the record
                var record = await _recordRepository.GetByIdWithLockAsync(command.LeaveRecordId, cancellationToken);
                if (record == null)
                {
                    return Result.Failure("Leave request not found.");
                }

                // 2. Require Status = Pending
                if (record.Status != LeaveRecordStatus.Pending)
                {
                    return Result.Failure("This request has already been reviewed.");
                }

                // 3. User cannot approve their own request
                var currentEmployeeId = _currentUser.EmployeeId;
                if (currentEmployeeId.HasValue && currentEmployeeId.Value == record.EmployeeId)
                {
                    return Result.Failure("You cannot approve your own leave request.");
                }

                // 4. Re-check overlap (ignoring this record)
                var hasOverlap = await _recordRepository.HasOverlappingRecordsAsync(
                    record.EmployeeId,
                    record.StartDate,
                    record.EndDate,
                    excludeRecordId: record.LeaveRecordId,
                    useLock: true,
                    cancellationToken: cancellationToken);

                if (hasOverlap)
                {
                    return Result.Failure("Approval refused: another approved or pending leave record now overlaps with this date range.");
                }

                // 5. Recalculate TotalDays through ICalendarQuery
                var totalDays = await _daysCalculator.CalculateTotalDaysAsync(
                    record.StartDate,
                    record.EndDate,
                    record.IsHalfDay,
                    cancellationToken);

                if (totalDays <= 0)
                {
                    return Result.Failure("Approval refused: the date range contains no working days (e.g. holidays or weekends).");
                }

                // 6. Create balance row if missing & check Remaining
                var leaveYear = await _yearHelper.GetLeaveYearAsync(record.StartDate, cancellationToken);
                var balance = await _yearHelper.GetOrCreateBalanceAsync(
                    record.EmployeeId,
                    record.LeaveTypeId,
                    leaveYear,
                    _currentUser.UserId,
                    cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                if (balance.Remaining < totalDays)
                {
                    return Result.Failure($"Approval refused: employee's remaining leave balance ({balance.Remaining:0.#} days) is insufficient for {totalDays:0.#} day(s).");
                }

                // 7. Update Record: Status = Approved, reviewer & timestamp, updated TotalDays
                var nowUtc = _clock.UtcNow;
                record.Status = LeaveRecordStatus.Approved;
                record.ReviewedByUserId = _currentUser.UserId;
                record.ReviewedAtUtc = nowUtc;
                record.TotalDays = totalDays;
                record.UpdatedAtUtc = nowUtc;
                record.UpdatedByUserId = _currentUser.UserId;

                await _recordRepository.UpdateAsync(record, cancellationToken);

                // 8. Increase Used on balance
                balance.Used += totalDays;
                balance.UpdatedAtUtc = nowUtc;
                balance.UpdatedByUserId = _currentUser.UserId;
                await _balanceRepository.UpdateAsync(balance, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                // 9. Call IAttendanceMarker to mark the days OnLeave
                await _attendanceMarker.MarkOnLeaveAsync(
                    record.EmployeeId,
                    record.StartDate,
                    record.EndDate,
                    record.IsHalfDay,
                    cancellationToken);

                await tx.CommitAsync(cancellationToken);

                return Result.Success();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken);
                return Result.Failure($"Failed to approve leave request: {ex.Message}");
            }
        });
    }
}

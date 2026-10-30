using Hrms.Attendance.Contracts;
using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Application.Services;
using Hrms.Leave.Domain.Enums;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Application.Commands.LeaveRecords;

public record CancelApprovedLeaveCommand(Guid LeaveRecordId);

public interface ICancelApprovedLeaveHandler
{
    Task<Result> HandleAsync(CancelApprovedLeaveCommand command, CancellationToken cancellationToken = default);
}

public class CancelApprovedLeaveHandler : ICancelApprovedLeaveHandler
{
    private readonly ILeaveDbContext _context;
    private readonly ILeaveRecordRepository _recordRepository;
    private readonly ILeaveBalanceRepository _balanceRepository;
    private readonly ILeaveYearHelper _yearHelper;
    private readonly IAttendanceMarker _attendanceMarker;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public CancelApprovedLeaveHandler(
        ILeaveDbContext context,
        ILeaveRecordRepository recordRepository,
        ILeaveBalanceRepository balanceRepository,
        ILeaveYearHelper yearHelper,
        IAttendanceMarker attendanceMarker,
        ICurrentUser currentUser,
        IClock clock)
    {
        _context = context;
        _recordRepository = recordRepository;
        _balanceRepository = balanceRepository;
        _yearHelper = yearHelper;
        _attendanceMarker = attendanceMarker;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(CancelApprovedLeaveCommand command, CancellationToken cancellationToken = default)
    {
        var dbContext = (DbContext)_context;
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var record = await _recordRepository.GetByIdWithLockAsync(command.LeaveRecordId, cancellationToken);
                if (record == null)
                {
                    return Result.Failure("Leave record not found.");
                }

                if (record.Status != LeaveRecordStatus.Approved)
                {
                    return Result.Failure($"Only approved leave records can be cancelled here. Current status is '{record.Status}'.");
                }

                var nowUtc = _clock.UtcNow;
                record.Status = LeaveRecordStatus.Cancelled;
                record.UpdatedAtUtc = nowUtc;
                record.UpdatedByUserId = _currentUser.UserId;

                await _recordRepository.UpdateAsync(record, cancellationToken);

                // Decrease Used on balance
                var leaveYear = await _yearHelper.GetLeaveYearAsync(record.StartDate, cancellationToken);
                var balance = await _balanceRepository.GetByEmployeeTypeYearAsync(
                    record.EmployeeId,
                    record.LeaveTypeId,
                    leaveYear,
                    cancellationToken);

                if (balance != null)
                {
                    balance.Used = Math.Max(0m, balance.Used - record.TotalDays);
                    balance.UpdatedAtUtc = nowUtc;
                    balance.UpdatedByUserId = _currentUser.UserId;
                    await _balanceRepository.UpdateAsync(balance, cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);

                // Call attendance marker to recalculate attendance days
                await _attendanceMarker.RecalculateDaysAsync(
                    record.EmployeeId,
                    record.StartDate,
                    record.EndDate,
                    cancellationToken);

                await tx.CommitAsync(cancellationToken);

                return Result.Success();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken);
                return Result.Failure($"Failed to cancel approved leave: {ex.Message}");
            }
        });
    }
}

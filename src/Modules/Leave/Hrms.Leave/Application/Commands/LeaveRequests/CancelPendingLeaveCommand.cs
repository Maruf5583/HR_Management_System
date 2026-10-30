using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Domain.Enums;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Application.Commands.LeaveRequests;

public record CancelPendingLeaveCommand(Guid LeaveRecordId);

public interface ICancelPendingLeaveHandler
{
    Task<Result> HandleAsync(CancelPendingLeaveCommand command, CancellationToken cancellationToken = default);
}

public class CancelPendingLeaveHandler : ICancelPendingLeaveHandler
{
    private readonly ILeaveDbContext _context;
    private readonly ILeaveRecordRepository _recordRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public CancelPendingLeaveHandler(
        ILeaveDbContext context,
        ILeaveRecordRepository recordRepository,
        ICurrentUser currentUser,
        IClock clock)
    {
        _context = context;
        _recordRepository = recordRepository;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(CancelPendingLeaveCommand command, CancellationToken cancellationToken = default)
    {
        var employeeId = _currentUser.EmployeeId;
        if (!employeeId.HasValue || employeeId.Value == Guid.Empty)
        {
            return Result.Failure("No linked employee record found for your user account.");
        }

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
                    return Result.Failure("Leave request not found.");
                }

                if (record.EmployeeId != employeeId.Value)
                {
                    return Result.Failure("You can only cancel your own leave requests.");
                }

                if (record.Status != LeaveRecordStatus.Pending)
                {
                    return Result.Failure($"Only pending requests can be cancelled. This request is currently '{record.Status}'.");
                }

                var nowUtc = _clock.UtcNow;
                record.Status = LeaveRecordStatus.Cancelled;
                record.UpdatedAtUtc = nowUtc;
                record.UpdatedByUserId = _currentUser.UserId;

                await _recordRepository.UpdateAsync(record, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                return Result.Success();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken);
                return Result.Failure($"Failed to cancel pending request: {ex.Message}");
            }
        });
    }
}

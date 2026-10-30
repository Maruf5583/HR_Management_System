using FluentValidation;
using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Domain.Enums;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Application.Commands.Approvals;

public record RejectLeaveRequestCommand(Guid LeaveRecordId, string ReviewNote);

public class RejectLeaveRequestCommandValidator : AbstractValidator<RejectLeaveRequestCommand>
{
    public RejectLeaveRequestCommandValidator()
    {
        RuleFor(x => x.LeaveRecordId).NotEmpty();
        RuleFor(x => x.ReviewNote)
            .NotEmpty().WithMessage("Rejection reason / review note is required.")
            .MaximumLength(500).WithMessage("Review note cannot exceed 500 characters.");
    }
}

public interface IRejectLeaveRequestHandler
{
    Task<Result> HandleAsync(RejectLeaveRequestCommand command, CancellationToken cancellationToken = default);
}

public class RejectLeaveRequestHandler : IRejectLeaveRequestHandler
{
    private readonly ILeaveDbContext _context;
    private readonly ILeaveRecordRepository _recordRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public RejectLeaveRequestHandler(
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

    public async Task<Result> HandleAsync(RejectLeaveRequestCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ReviewNote))
        {
            return Result.Failure("Review note is required when rejecting a request.");
        }

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

                // 3. User cannot reject their own request
                var currentEmployeeId = _currentUser.EmployeeId;
                if (currentEmployeeId.HasValue && currentEmployeeId.Value == record.EmployeeId)
                {
                    return Result.Failure("You cannot reject your own leave request.");
                }

                // 4. Update status to Rejected with reviewer and timestamp. Do NOT change Used or call IAttendanceMarker.
                var nowUtc = _clock.UtcNow;
                record.Status = LeaveRecordStatus.Rejected;
                record.ReviewedByUserId = _currentUser.UserId;
                record.ReviewedAtUtc = nowUtc;
                record.ReviewNote = command.ReviewNote.Trim();
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
                return Result.Failure($"Failed to reject leave request: {ex.Message}");
            }
        });
    }
}

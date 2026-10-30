using FluentValidation;
using Hrms.Attendance.Contracts;
using Hrms.Employees.Contracts;
using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Application.Services;
using Hrms.Leave.Domain.Entities;
using Hrms.Leave.Domain.Enums;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Application.Commands.LeaveRecords;

public record RecordDirectLeaveCommand(
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsHalfDay,
    string? Reason
);

public class RecordDirectLeaveCommandValidator : AbstractValidator<RecordDirectLeaveCommand>
{
    public RecordDirectLeaveCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("Please select an employee.");
        RuleFor(x => x.LeaveTypeId).NotEmpty().WithMessage("Please select a leave type.");
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("Start date is required.");
        RuleFor(x => x.EndDate).NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date cannot be earlier than start date.");

        When(x => x.IsHalfDay, () =>
        {
            RuleFor(x => x.EndDate).Equal(x => x.StartDate)
                .WithMessage("Half-day leave must start and end on the same date.");
        });

        RuleFor(x => x.Reason).MaximumLength(1000).WithMessage("Reason cannot exceed 1000 characters.");
    }
}

public interface IRecordDirectLeaveHandler
{
    Task<Result<Guid>> HandleAsync(RecordDirectLeaveCommand command, CancellationToken cancellationToken = default);
}

public class RecordDirectLeaveHandler : IRecordDirectLeaveHandler
{
    private readonly ILeaveDbContext _context;
    private readonly ILeaveRecordRepository _recordRepository;
    private readonly ILeaveBalanceRepository _balanceRepository;
    private readonly ILeaveTypeRepository _typeRepository;
    private readonly ILeaveYearHelper _yearHelper;
    private readonly ILeaveDaysCalculator _daysCalculator;
    private readonly IEmployeeLookup _employeeLookup;
    private readonly IAttendanceMarker _attendanceMarker;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public RecordDirectLeaveHandler(
        ILeaveDbContext context,
        ILeaveRecordRepository recordRepository,
        ILeaveBalanceRepository balanceRepository,
        ILeaveTypeRepository typeRepository,
        ILeaveYearHelper yearHelper,
        ILeaveDaysCalculator daysCalculator,
        IEmployeeLookup employeeLookup,
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
        _employeeLookup = employeeLookup;
        _attendanceMarker = attendanceMarker;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> HandleAsync(RecordDirectLeaveCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Employee must be active
        var emp = await _employeeLookup.GetBasicInfoAsync(command.EmployeeId, cancellationToken);
        if (emp == null || !string.Equals(emp.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return Result<Guid>.Failure("Employee not found or is not active.");
        }

        // 2. Validate dates
        if (command.EndDate < command.StartDate)
        {
            return Result<Guid>.Failure("End date cannot be earlier than start date.");
        }

        if (command.IsHalfDay && command.StartDate != command.EndDate)
        {
            return Result<Guid>.Failure("Half-day leave must start and end on the same date.");
        }

        var leaveType = await _typeRepository.GetByIdAsync(command.LeaveTypeId, cancellationToken);
        if (leaveType == null || leaveType.Status != LeaveTypeStatus.Active)
        {
            return Result<Guid>.Failure("Selected leave type is not active or does not exist.");
        }

        var dbContext = (DbContext)_context;
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 3. Overlap check with Pending or Approved records
                var hasOverlap = await _recordRepository.HasOverlappingRecordsAsync(
                    command.EmployeeId,
                    command.StartDate,
                    command.EndDate,
                    excludeRecordId: null,
                    useLock: true,
                    cancellationToken: cancellationToken);

                if (hasOverlap)
                {
                    return Result<Guid>.Failure("This employee already has a pending or approved leave record covering the selected dates.");
                }

                // 4. Calculate total days
                var totalDays = await _daysCalculator.CalculateTotalDaysAsync(
                    command.StartDate,
                    command.EndDate,
                    command.IsHalfDay,
                    cancellationToken);

                if (totalDays <= 0)
                {
                    return Result<Guid>.Failure("The selected date range contains no working days (all selected days are weekly off or holidays).");
                }

                // 5. Balance check & auto-create missing row
                var leaveYear = await _yearHelper.GetLeaveYearAsync(command.StartDate, cancellationToken);
                var balance = await _yearHelper.GetOrCreateBalanceAsync(
                    command.EmployeeId,
                    command.LeaveTypeId,
                    leaveYear,
                    _currentUser.UserId,
                    cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                if (balance.Remaining < totalDays)
                {
                    return Result<Guid>.Failure($"Insufficient leave balance. Remaining: {balance.Remaining:0.#} day(s), Required: {totalDays:0.#} day(s).");
                }

                var nowUtc = _clock.UtcNow;
                var record = LeaveRecord.CreateApprovedDirect(
                    command.EmployeeId,
                    command.LeaveTypeId,
                    command.StartDate,
                    command.EndDate,
                    command.IsHalfDay,
                    totalDays,
                    command.Reason,
                    _currentUser.UserId,
                    nowUtc);

                await _recordRepository.AddAsync(record, cancellationToken);

                // 6. Increase Used on balance
                balance.Used += totalDays;
                balance.UpdatedAtUtc = nowUtc;
                balance.UpdatedByUserId = _currentUser.UserId;
                await _balanceRepository.UpdateAsync(balance, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                // 7. Call attendance marker
                await _attendanceMarker.MarkOnLeaveAsync(
                    command.EmployeeId,
                    command.StartDate,
                    command.EndDate,
                    command.IsHalfDay,
                    cancellationToken);

                await tx.CommitAsync(cancellationToken);

                return Result<Guid>.Success(record.LeaveRecordId);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken);
                return Result<Guid>.Failure($"Failed to record leave: {ex.Message}");
            }
        });
    }
}

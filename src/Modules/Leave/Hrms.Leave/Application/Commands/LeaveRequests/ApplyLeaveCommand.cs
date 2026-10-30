using FluentValidation;
using Hrms.Calendar.Contracts;
using Hrms.Employees.Contracts;
using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Application.Services;
using Hrms.Leave.Domain.Entities;
using Hrms.Leave.Domain.Enums;
using Hrms.Settings.Contracts;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Application.Commands.LeaveRequests;

public record ApplyLeaveCommand(
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsHalfDay,
    string? Reason
);

public class ApplyLeaveCommandValidator : AbstractValidator<ApplyLeaveCommand>
{
    public ApplyLeaveCommandValidator()
    {
        RuleFor(x => x.LeaveTypeId)
            .NotEmpty().WithMessage("Please select a leave type.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date cannot be earlier than start date.");

        When(x => x.IsHalfDay, () =>
        {
            RuleFor(x => x.EndDate)
                .Equal(x => x.StartDate)
                .WithMessage("Half-day leave must start and end on the same date.");
        });

        RuleFor(x => x.Reason)
            .MaximumLength(1000).WithMessage("Reason cannot exceed 1000 characters.");
    }
}

public interface IApplyLeaveHandler
{
    Task<Result<Guid>> HandleAsync(ApplyLeaveCommand command, CancellationToken cancellationToken = default);
}

public class ApplyLeaveHandler : IApplyLeaveHandler
{
    private readonly ILeaveDbContext _context;
    private readonly ILeaveRecordRepository _recordRepository;
    private readonly ILeaveTypeRepository _typeRepository;
    private readonly ILeaveYearHelper _yearHelper;
    private readonly ILeaveDaysCalculator _daysCalculator;
    private readonly IEmployeeLookup _employeeLookup;
    private readonly ISettingsReader _settingsReader;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public ApplyLeaveHandler(
        ILeaveDbContext context,
        ILeaveRecordRepository recordRepository,
        ILeaveTypeRepository typeRepository,
        ILeaveYearHelper yearHelper,
        ILeaveDaysCalculator daysCalculator,
        IEmployeeLookup employeeLookup,
        ISettingsReader settingsReader,
        ICurrentUser currentUser,
        IClock clock)
    {
        _context = context;
        _recordRepository = recordRepository;
        _typeRepository = typeRepository;
        _yearHelper = yearHelper;
        _daysCalculator = daysCalculator;
        _employeeLookup = employeeLookup;
        _settingsReader = settingsReader;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> HandleAsync(ApplyLeaveCommand command, CancellationToken cancellationToken = default)
    {
        var employeeId = _currentUser.EmployeeId;
        if (!employeeId.HasValue || employeeId.Value == Guid.Empty)
        {
            return Result<Guid>.Failure("No linked employee record found for your user account.");
        }

        // 1. Employee must be active
        var empInfo = await _employeeLookup.GetBasicInfoAsync(employeeId.Value, cancellationToken);
        if (empInfo == null || !string.Equals(empInfo.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return Result<Guid>.Failure("Only active employees can apply for leave.");
        }

        // 2. Start date must be today or later in company local timezone
        var today = _clock.TodayLocal;
        if (command.StartDate < today)
        {
            return Result<Guid>.Failure($"Start date cannot be in the past. Today is {today:yyyy-MM-dd}.");
        }

        if (command.EndDate < command.StartDate)
        {
            return Result<Guid>.Failure("End date cannot be earlier than start date.");
        }

        if (command.IsHalfDay && command.StartDate != command.EndDate)
        {
            return Result<Guid>.Failure("Half-day leave must start and end on the same date.");
        }

        // 3. Active leave type check
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
                // 4. Overlap check with UPDLOCK, HOLDLOCK
                var hasOverlap = await _recordRepository.HasOverlappingRecordsAsync(
                    employeeId.Value,
                    command.StartDate,
                    command.EndDate,
                    excludeRecordId: null,
                    useLock: true,
                    cancellationToken: cancellationToken);

                if (hasOverlap)
                {
                    return Result<Guid>.Failure("You already have a pending or approved leave request covering these dates.");
                }

                // 5. Calculate TotalDays (weekends and holidays excluded; half day = 0.5)
                var totalDays = await _daysCalculator.CalculateTotalDaysAsync(
                    command.StartDate,
                    command.EndDate,
                    command.IsHalfDay,
                    cancellationToken);

                if (totalDays <= 0)
                {
                    return Result<Guid>.Failure("The selected date range contains no working days (all selected days are weekly off or holidays).");
                }

                // 6. Balance check (create balance row if missing)
                var leaveYear = await _yearHelper.GetLeaveYearAsync(command.StartDate, cancellationToken);
                var balance = await _yearHelper.GetOrCreateBalanceAsync(
                    employeeId.Value,
                    command.LeaveTypeId,
                    leaveYear,
                    _currentUser.UserId,
                    cancellationToken);

                // Save if newly created so Remaining is computed/available
                await _context.SaveChangesAsync(cancellationToken);

                if (balance.Remaining < totalDays)
                {
                    return Result<Guid>.Failure($"Insufficient leave balance. You have {balance.Remaining:0.#} day(s) remaining for {leaveType.Name}, but requested {totalDays:0.#} day(s).");
                }

                var nowUtc = _clock.UtcNow;
                var record = new LeaveRecord
                {
                    LeaveRecordId = Guid.NewGuid(),
                    EmployeeId = employeeId.Value,
                    LeaveTypeId = command.LeaveTypeId,
                    StartDate = command.StartDate,
                    EndDate = command.EndDate,
                    IsHalfDay = command.IsHalfDay,
                    TotalDays = totalDays,
                    Reason = command.Reason?.Trim(),
                    Status = LeaveRecordStatus.Pending,
                    CreatedAtUtc = nowUtc,
                    CreatedByUserId = _currentUser.UserId,
                    UpdatedAtUtc = nowUtc,
                    UpdatedByUserId = _currentUser.UserId
                };

                await _recordRepository.AddAsync(record, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                return Result<Guid>.Success(record.LeaveRecordId);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken);
                return Result<Guid>.Failure($"Failed to apply for leave: {ex.Message}");
            }
        });
    }
}

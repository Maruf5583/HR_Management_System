using FluentValidation;
using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Domain.Enums;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;

namespace Hrms.Leave.Application.Commands.LeaveTypes;

public record UpdateLeaveTypeCommand(
    Guid LeaveTypeId,
    string Name,
    string Code,
    bool IsPaid,
    decimal DaysPerYear,
    bool RequiresDocument
);

public class UpdateLeaveTypeCommandValidator : AbstractValidator<UpdateLeaveTypeCommand>
{
    public UpdateLeaveTypeCommandValidator()
    {
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DaysPerYear).GreaterThanOrEqualTo(0m);
    }
}

public interface IUpdateLeaveTypeHandler
{
    Task<Result> HandleAsync(UpdateLeaveTypeCommand command, CancellationToken cancellationToken = default);
}

public class UpdateLeaveTypeHandler : IUpdateLeaveTypeHandler
{
    private readonly ILeaveDbContext _context;
    private readonly ILeaveTypeRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public UpdateLeaveTypeHandler(
        ILeaveDbContext context,
        ILeaveTypeRepository repository,
        ICurrentUser currentUser,
        IClock clock)
    {
        _context = context;
        _repository = repository;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(UpdateLeaveTypeCommand command, CancellationToken cancellationToken = default)
    {
        var leaveType = await _repository.GetByIdAsync(command.LeaveTypeId, cancellationToken);
        if (leaveType == null)
        {
            return Result.Failure("Leave type not found.");
        }

        if (await _repository.ExistsByNameAsync(command.Name, command.LeaveTypeId, cancellationToken))
        {
            return Result.Failure($"Leave type with name '{command.Name}' already exists.");
        }

        if (await _repository.ExistsByCodeAsync(command.Code, command.LeaveTypeId, cancellationToken))
        {
            return Result.Failure($"Leave type with code '{command.Code}' already exists.");
        }

        leaveType.Name = command.Name.Trim();
        leaveType.Code = command.Code.Trim().ToUpperInvariant();
        leaveType.IsPaid = command.IsPaid;
        leaveType.DaysPerYear = command.DaysPerYear;
        leaveType.RequiresDocument = command.RequiresDocument;
        leaveType.UpdatedAtUtc = _clock.UtcNow;
        leaveType.UpdatedByUserId = _currentUser.UserId;

        await _repository.UpdateAsync(leaveType, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public record ToggleLeaveTypeStatusCommand(Guid LeaveTypeId);

public interface IToggleLeaveTypeStatusHandler
{
    Task<Result> HandleAsync(ToggleLeaveTypeStatusCommand command, CancellationToken cancellationToken = default);
}

public class ToggleLeaveTypeStatusHandler : IToggleLeaveTypeStatusHandler
{
    private readonly ILeaveDbContext _context;
    private readonly ILeaveTypeRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public ToggleLeaveTypeStatusHandler(
        ILeaveDbContext context,
        ILeaveTypeRepository repository,
        ICurrentUser currentUser,
        IClock clock)
    {
        _context = context;
        _repository = repository;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(ToggleLeaveTypeStatusCommand command, CancellationToken cancellationToken = default)
    {
        var leaveType = await _repository.GetByIdAsync(command.LeaveTypeId, cancellationToken);
        if (leaveType == null)
        {
            return Result.Failure("Leave type not found.");
        }

        leaveType.Status = leaveType.Status == LeaveTypeStatus.Active
            ? LeaveTypeStatus.Inactive
            : LeaveTypeStatus.Active;

        leaveType.UpdatedAtUtc = _clock.UtcNow;
        leaveType.UpdatedByUserId = _currentUser.UserId;

        await _repository.UpdateAsync(leaveType, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

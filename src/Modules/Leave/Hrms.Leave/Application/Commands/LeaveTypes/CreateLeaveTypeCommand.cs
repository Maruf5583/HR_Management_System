using FluentValidation;
using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Domain.Entities;
using Hrms.Leave.Domain.Enums;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;

namespace Hrms.Leave.Application.Commands.LeaveTypes;

public record CreateLeaveTypeCommand(
    string Name,
    string Code,
    bool IsPaid,
    decimal DaysPerYear,
    bool RequiresDocument
);

public class CreateLeaveTypeCommandValidator : AbstractValidator<CreateLeaveTypeCommand>
{
    public CreateLeaveTypeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(20).WithMessage("Code cannot exceed 20 characters.");

        RuleFor(x => x.DaysPerYear)
            .GreaterThanOrEqualTo(0m).WithMessage("Days per year must be 0 or more.");
    }
}

public interface ICreateLeaveTypeHandler
{
    Task<Result<Guid>> HandleAsync(CreateLeaveTypeCommand command, CancellationToken cancellationToken = default);
}

public class CreateLeaveTypeHandler : ICreateLeaveTypeHandler
{
    private readonly ILeaveDbContext _context;
    private readonly ILeaveTypeRepository _repository;
    private readonly ICurrentUser _currentUser;

    public CreateLeaveTypeHandler(
        ILeaveDbContext context,
        ILeaveTypeRepository repository,
        ICurrentUser currentUser)
    {
        _context = context;
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> HandleAsync(CreateLeaveTypeCommand command, CancellationToken cancellationToken = default)
    {
        if (await _repository.ExistsByNameAsync(command.Name, cancellationToken: cancellationToken))
        {
            return Result<Guid>.Failure($"Leave type with name '{command.Name}' already exists.");
        }

        if (await _repository.ExistsByCodeAsync(command.Code, cancellationToken: cancellationToken))
        {
            return Result<Guid>.Failure($"Leave type with code '{command.Code}' already exists.");
        }

        var leaveType = LeaveType.Create(
            command.Name,
            command.Code,
            command.IsPaid,
            command.DaysPerYear,
            command.RequiresDocument,
            _currentUser.UserId);

        await _repository.AddAsync(leaveType, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(leaveType.LeaveTypeId);
    }
}

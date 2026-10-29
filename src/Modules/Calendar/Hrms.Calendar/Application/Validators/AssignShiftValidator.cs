using FluentValidation;
using Hrms.Calendar.Application.Models;

namespace Hrms.Calendar.Application.Validators;

public class AssignShiftValidator : AbstractValidator<AssignShiftCommand>
{
    public AssignShiftValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee is required.");

        RuleFor(x => x.ShiftId)
            .NotEmpty().WithMessage("Shift is required.");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("Effective from date is required.");

        RuleFor(x => x)
            .Must(x => x.EffectiveTo == null || x.EffectiveTo >= x.EffectiveFrom)
            .WithMessage("Effective to date must be on or after effective from date.");
    }
}

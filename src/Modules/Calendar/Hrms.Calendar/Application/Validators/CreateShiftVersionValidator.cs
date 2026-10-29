using FluentValidation;
using Hrms.Calendar.Application.Models;

namespace Hrms.Calendar.Application.Validators;

public class CreateShiftVersionValidator : AbstractValidator<CreateShiftVersionCommand>
{
    public CreateShiftVersionValidator()
    {
        RuleFor(x => x.ShiftId)
            .NotEmpty().WithMessage("Shift ID is required.");

        RuleFor(x => x.GraceMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Grace minutes must be 0 or greater.");

        RuleFor(x => x.BreakMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Break minutes must be 0 or greater.");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("Effective from date is required.");
    }
}

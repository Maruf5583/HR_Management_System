using FluentValidation;
using Hrms.Calendar.Application.Models;

namespace Hrms.Calendar.Application.Validators;

public class CreateShiftValidator : AbstractValidator<CreateShiftCommand>
{
    public CreateShiftValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Shift name is required.")
            .MaximumLength(100).WithMessage("Shift name must not exceed 100 characters.");

        RuleFor(x => x.GraceMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Grace minutes must be 0 or greater.");

        RuleFor(x => x.BreakMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Break minutes must be 0 or greater.");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("Effective from date is required.");
    }
}

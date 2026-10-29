using FluentValidation;
using Hrms.Calendar.Application.Models;

namespace Hrms.Calendar.Application.Validators;

public class CreateMultiDayHolidayValidator : AbstractValidator<CreateMultiDayHolidayCommand>
{
    public CreateMultiDayHolidayValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Holiday name is required.")
            .MaximumLength(120).WithMessage("Holiday name must not exceed 120 characters.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be on or after start date.");
    }
}

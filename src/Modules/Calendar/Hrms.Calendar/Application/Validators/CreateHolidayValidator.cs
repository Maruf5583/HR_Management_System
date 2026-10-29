using FluentValidation;
using Hrms.Calendar.Application.Models;

namespace Hrms.Calendar.Application.Validators;

public class CreateHolidayValidator : AbstractValidator<CreateHolidayCommand>
{
    public CreateHolidayValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Holiday name is required.")
            .MaximumLength(120).WithMessage("Holiday name must not exceed 120 characters.");

        RuleFor(x => x.HolidayDate)
            .NotEmpty().WithMessage("Holiday date is required.");
    }
}

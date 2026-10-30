using FluentValidation;
using Hrms.Assets.Application.Models;

namespace Hrms.Assets.Application.Validators;

public class AssignAssetValidator : AbstractValidator<AssignAssetCommand>
{
    public AssignAssetValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty().WithMessage("Asset ID is required.");

        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee selection is required.");

        RuleFor(x => x.AssignedAtUtc)
            .NotEmpty().WithMessage("Assignment date is required.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note cannot exceed 500 characters.");
    }
}

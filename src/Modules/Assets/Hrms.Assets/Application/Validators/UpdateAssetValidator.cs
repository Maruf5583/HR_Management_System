using FluentValidation;
using Hrms.Assets.Application.Models;

namespace Hrms.Assets.Application.Validators;

public class UpdateAssetValidator : AbstractValidator<UpdateAssetCommand>
{
    public UpdateAssetValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty().WithMessage("Asset ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Asset name is required.")
            .MaximumLength(150).WithMessage("Asset name cannot exceed 150 characters.");

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100).WithMessage("Serial number cannot exceed 100 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("Concurrency token (RowVersion) is required.");
    }
}

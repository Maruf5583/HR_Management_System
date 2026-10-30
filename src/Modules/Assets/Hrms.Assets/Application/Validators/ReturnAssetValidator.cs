using FluentValidation;
using Hrms.Assets.Application.Models;

namespace Hrms.Assets.Application.Validators;

public class ReturnAssetValidator : AbstractValidator<ReturnAssetCommand>
{
    public ReturnAssetValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty().WithMessage("Asset ID is required.");

        RuleFor(x => x.ReturnedAtUtc)
            .NotEmpty().WithMessage("Return date is required.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note cannot exceed 500 characters.");
    }
}

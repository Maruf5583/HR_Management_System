using FluentValidation;
using Hrms.Employees.Application.Models;

namespace Hrms.Employees.Application.Validators;

public class SubmitBankDetailValidator : AbstractValidator<SubmitBankDetailModel>
{
    public SubmitBankDetailValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee is required.");

        RuleFor(x => x.BankName)
            .NotEmpty().WithMessage("Bank name is required.")
            .MaximumLength(100).WithMessage("Bank name cannot exceed 100 characters.");

        RuleFor(x => x.AccountHolderName)
            .NotEmpty().WithMessage("Account holder name is required.")
            .MaximumLength(100).WithMessage("Account holder name cannot exceed 100 characters.");

        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Account number is required.")
            .MaximumLength(50).WithMessage("Account number cannot exceed 50 characters.");

        RuleFor(x => x.BranchName)
            .NotEmpty().WithMessage("Branch name is required.")
            .MaximumLength(100).WithMessage("Branch name cannot exceed 100 characters.");

        RuleFor(x => x.RoutingNumber)
            .MaximumLength(50).WithMessage("Routing number cannot exceed 50 characters.");

        RuleFor(x => x.SwiftCode)
            .MaximumLength(50).WithMessage("Swift code cannot exceed 50 characters.");
    }
}

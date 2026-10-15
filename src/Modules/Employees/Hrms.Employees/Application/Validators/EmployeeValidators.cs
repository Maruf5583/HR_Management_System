using FluentValidation;
using Hrms.Employees.Application.Models;

namespace Hrms.Employees.Application.Validators;

public class CreateEmployeeModelValidator : AbstractValidator<CreateEmployeeModel>
{
    public CreateEmployeeModelValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("Invalid email address format.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("Phone number cannot exceed 30 characters.");

        RuleFor(x => x.Gender)
            .MaximumLength(20).WithMessage("Gender cannot exceed 20 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address cannot exceed 500 characters.");

        RuleFor(x => x.NidNumber)
            .MaximumLength(50).WithMessage("NID number cannot exceed 50 characters.");

        RuleFor(x => x.JoinDate)
            .NotEmpty().WithMessage("Join date is required.");

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department is required.");

        RuleFor(x => x.DesignationId)
            .NotEmpty().WithMessage("Designation is required.");

        RuleFor(x => x.GradeId)
            .NotEmpty().WithMessage("Grade is required.");

        RuleFor(x => x.EmergencyContactName)
            .MaximumLength(150).WithMessage("Emergency contact name cannot exceed 150 characters.");

        RuleFor(x => x.EmergencyContactPhone)
            .MaximumLength(30).WithMessage("Emergency contact phone cannot exceed 30 characters.");
    }
}

public class UpdateEmployeeModelValidator : AbstractValidator<UpdateEmployeeModel>
{
    public UpdateEmployeeModelValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee ID is required.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("Row version is required for concurrency control.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("Invalid email address format.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("Phone number cannot exceed 30 characters.");

        RuleFor(x => x.Gender)
            .MaximumLength(20).WithMessage("Gender cannot exceed 20 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address cannot exceed 500 characters.");

        RuleFor(x => x.NidNumber)
            .MaximumLength(50).WithMessage("NID number cannot exceed 50 characters.");

        RuleFor(x => x.JoinDate)
            .NotEmpty().WithMessage("Join date is required.");

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department is required.");

        RuleFor(x => x.DesignationId)
            .NotEmpty().WithMessage("Designation is required.");

        RuleFor(x => x.GradeId)
            .NotEmpty().WithMessage("Grade is required.");

        RuleFor(x => x.EmergencyContactName)
            .MaximumLength(150).WithMessage("Emergency contact name cannot exceed 150 characters.");

        RuleFor(x => x.EmergencyContactPhone)
            .MaximumLength(30).WithMessage("Emergency contact phone cannot exceed 30 characters.");
    }
}

public class ChangeEmployeeStatusModelValidator : AbstractValidator<ChangeEmployeeStatusModel>
{
    public ChangeEmployeeStatusModelValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee ID is required.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("Row version is required for concurrency control.");
    }
}

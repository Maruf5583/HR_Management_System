using FluentValidation;
using Hrms.Employees.Application.Models;

namespace Hrms.Employees.Application.Validators;

public class CreateDepartmentModelValidator : AbstractValidator<CreateDepartmentModel>
{
    public CreateDepartmentModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Department name is required.")
            .MaximumLength(100).WithMessage("Department name cannot exceed 100 characters.");
    }
}

public class UpdateDepartmentModelValidator : AbstractValidator<UpdateDepartmentModel>
{
    public UpdateDepartmentModelValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Department ID is required.");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Department name is required.")
            .MaximumLength(100).WithMessage("Department name cannot exceed 100 characters.");
    }
}

public class CreateDesignationModelValidator : AbstractValidator<CreateDesignationModel>
{
    public CreateDesignationModelValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Designation title is required.")
            .MaximumLength(120).WithMessage("Designation title cannot exceed 120 characters.");
        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department is required.");
    }
}

public class UpdateDesignationModelValidator : AbstractValidator<UpdateDesignationModel>
{
    public UpdateDesignationModelValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Designation ID is required.");
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Designation title is required.")
            .MaximumLength(120).WithMessage("Designation title cannot exceed 120 characters.");
        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department is required.");
    }
}

public class CreateGradeModelValidator : AbstractValidator<CreateGradeModel>
{
    public CreateGradeModelValidator()
    {
        RuleFor(x => x.DesignationId).NotEmpty().WithMessage("Designation is required.");
        RuleFor(x => x.Level)
            .NotEmpty().WithMessage("Level is required.")
            .MaximumLength(30).WithMessage("Level cannot exceed 30 characters.");
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Grade code is required.")
            .MaximumLength(30).WithMessage("Grade code cannot exceed 30 characters.");
        RuleFor(x => x.Salary)
            .GreaterThanOrEqualTo(0).WithMessage("Salary must be greater than or equal to 0.");
    }
}

public class UpdateGradeModelValidator : AbstractValidator<UpdateGradeModel>
{
    public UpdateGradeModelValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Grade ID is required.");
        RuleFor(x => x.DesignationId).NotEmpty().WithMessage("Designation is required.");
        RuleFor(x => x.Level)
            .NotEmpty().WithMessage("Level is required.")
            .MaximumLength(30).WithMessage("Level cannot exceed 30 characters.");
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Grade code is required.")
            .MaximumLength(30).WithMessage("Grade code cannot exceed 30 characters.");
        RuleFor(x => x.Salary)
            .GreaterThanOrEqualTo(0).WithMessage("Salary must be greater than or equal to 0.");
    }
}

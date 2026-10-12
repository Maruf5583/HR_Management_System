using FluentValidation;
using Hrms.Identity.Application.Models;
using Hrms.Identity.Domain.Entities;

namespace Hrms.Identity.Application.Validators;

public class CreateUserModelValidator : AbstractValidator<CreateUserModel>
{
    public CreateUserModelValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");

        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role is required.");
    }
}

public class UpdateUserModelValidator : AbstractValidator<UpdateUserModel>
{
    public UpdateUserModelValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role is required.");
    }
}

public class CreateRoleModelValidator : AbstractValidator<CreateRoleModel>
{
    public CreateRoleModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(50).WithMessage("Role name cannot exceed 50 characters.")
            .Must(name => !ApplicationRole.IsSystemRole(name))
            .WithMessage("Cannot use reserved system role names.");
    }
}

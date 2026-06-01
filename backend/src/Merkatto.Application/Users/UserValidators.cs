using FluentValidation;
using Merkatto.Domain.Auth;

namespace Merkatto.Application.Users;

public sealed class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres.");
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(120)
            .When(x => x.Role == Role.Encargado)
            .WithMessage("Indicá el nombre del negocio del encargado.");
    }
}

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(120)
            .When(x => x.Role == Role.Encargado)
            .WithMessage("Indicá el nombre del negocio del encargado.");
    }
}

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres.");
    }
}

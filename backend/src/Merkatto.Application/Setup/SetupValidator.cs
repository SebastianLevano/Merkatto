using FluentValidation;

namespace Merkatto.Application.Setup;

public sealed class InitializeRequestValidator : AbstractValidator<InitializeRequest>
{
    public InitializeRequestValidator()
    {
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}

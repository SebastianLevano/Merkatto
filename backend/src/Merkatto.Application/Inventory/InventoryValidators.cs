using FluentValidation;

namespace Merkatto.Application.Inventory;

public sealed class TransferValidator : AbstractValidator<TransferRequest>
{
    public TransferValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(300);
    }
}

public sealed class AdjustmentValidator : AbstractValidator<AdjustmentRequest>
{
    public AdjustmentValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).NotEqual(0);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Location).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(300);
    }
}

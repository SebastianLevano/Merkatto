using FluentValidation;

namespace Merkatto.Application.Catalog;

public sealed class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.InternalCode).MaximumLength(64);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.PurchaseUnit).NotEmpty().MaximumLength(40);
        RuleFor(x => x.SaleUnit).NotEmpty().MaximumLength(40);
        RuleFor(x => x.UnitsPerPurchaseUnit).GreaterThanOrEqualTo(1);
        RuleFor(x => x.LastPurchaseCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.InitialWarehouseStock).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.InternalCode).MaximumLength(64);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.PurchaseUnit).NotEmpty().MaximumLength(40);
        RuleFor(x => x.SaleUnit).NotEmpty().MaximumLength(40);
        RuleFor(x => x.UnitsPerPurchaseUnit).GreaterThanOrEqualTo(1);
        RuleFor(x => x.LastPurchaseCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0);
    }
}

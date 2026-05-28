using FluentValidation;

namespace Merkatto.Application.Catalog;

public sealed class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.InternalCode).MaximumLength(64);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.SaleUnit).NotEmpty().MaximumLength(40);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0);

        // Initial-load block: optional, but if any field is provided, paquetes and units are required.
        When(x => x.InitialPaquetes is not null || x.InitialUnidadesPorPaquete is not null || x.InitialCostoPorPaquete is not null,
            () =>
            {
                RuleFor(x => x.InitialPaquetes).NotNull().GreaterThan(0)
                    .WithMessage("Indica cuántos paquetes tienes en stock.");
                RuleFor(x => x.InitialUnidadesPorPaquete).NotNull().GreaterThanOrEqualTo(1)
                    .WithMessage("Indica cuántas unidades trae cada paquete.");
                RuleFor(x => x.InitialCostoPorPaquete!.Value).GreaterThanOrEqualTo(0)
                    .When(x => x.InitialCostoPorPaquete is not null);
            });
    }
}

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.InternalCode).MaximumLength(64);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.SaleUnit).NotEmpty().MaximumLength(40);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0);
    }
}

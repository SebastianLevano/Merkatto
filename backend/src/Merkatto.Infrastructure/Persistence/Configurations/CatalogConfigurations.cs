using Merkatto.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Merkatto.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.Property(c => c.Name).IsRequired().HasMaxLength(120);
        b.HasIndex(c => c.Name);
    }
}

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> b)
    {
        b.Property(c => c.Name).IsRequired().HasMaxLength(120);
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.Property(p => p.Name).IsRequired().HasMaxLength(200);
        b.Property(p => p.InternalCode).HasMaxLength(64);
        b.Property(p => p.PurchaseUnit).IsRequired().HasMaxLength(40);
        b.Property(p => p.SaleUnit).IsRequired().HasMaxLength(40);

        // Stock & quantities use 3 decimals to allow fractional sale units.
        b.Property(p => p.WarehouseStock).HasPrecision(18, 3);
        b.Property(p => p.CounterStock).HasPrecision(18, 3);
        b.Property(p => p.MinStock).HasPrecision(18, 3);

        b.HasIndex(p => p.Name);
        b.HasIndex(p => p.InternalCode);

        b.HasOne(p => p.Category).WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.Brand).WithMany(c => c.Products)
            .HasForeignKey(p => p.BrandId).OnDelete(DeleteBehavior.SetNull);

        // Computed properties are not persisted.
        b.Ignore(p => p.UnitCost);
        b.Ignore(p => p.Margin);
        b.Ignore(p => p.MarginRate);
        b.Ignore(p => p.TotalStock);
        b.Ignore(p => p.IsLowStock);
    }
}

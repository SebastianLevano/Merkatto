using Merkatto.Domain.Credit;
using Merkatto.Domain.Inventory;
using Merkatto.Domain.Operations;
using Merkatto.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Merkatto.Infrastructure.Persistence.Configurations;

public sealed class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> b)
    {
        b.Property(i => i.PurchaseUnit).IsRequired().HasMaxLength(40);
        b.Property(i => i.Quantity).HasPrecision(18, 3);
        b.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.Ignore(i => i.Subtotal);
        b.Ignore(i => i.QuantityInSaleUnits);
    }
}

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> b)
    {
        b.Property(m => m.Quantity).HasPrecision(18, 3);
        b.Property(m => m.MovementType).HasConversion<int>();
        b.Property(m => m.Location).HasConversion<int>();
        b.HasIndex(m => new { m.ProductId, m.OccurredAt });
        b.HasOne(m => m.Product).WithMany().HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> b)
    {
        b.Property(a => a.Quantity).HasPrecision(18, 3);
        b.Property(a => a.Type).HasConversion<int>();
        b.Property(a => a.Location).HasConversion<int>();
        b.HasOne(a => a.Product).WithMany().HasForeignKey(a => a.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DailyClosingConfiguration : IEntityTypeConfiguration<DailyClosing>
{
    public void Configure(EntityTypeBuilder<DailyClosing> b)
    {
        b.Property(c => c.PosCommissionRate).HasPrecision(6, 4);
        b.HasIndex(c => c.BusinessDate).IsUnique();
        b.Ignore(c => c.PosCommissionAmount);
        b.Ignore(c => c.GrossIncome);
        b.Ignore(c => c.NetFlow);
    }
}

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> b)
    {
        b.Property(e => e.Type).HasConversion<int>();
        b.Property(e => e.Description).HasMaxLength(300);
        b.HasIndex(e => e.Date);
        b.HasOne(e => e.DailyClosing).WithMany().HasForeignKey(e => e.DailyClosingId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class CreditConfigurations :
    IEntityTypeConfiguration<CreditCustomer>,
    IEntityTypeConfiguration<CreditSale>,
    IEntityTypeConfiguration<CreditSaleItem>,
    IEntityTypeConfiguration<CreditPayment>
{
    public void Configure(EntityTypeBuilder<CreditCustomer> b)
    {
        b.Property(c => c.Name).IsRequired().HasMaxLength(160);
        b.Property(c => c.Phone).HasMaxLength(30);
        b.HasIndex(c => c.Name);
    }

    public void Configure(EntityTypeBuilder<CreditSale> b)
    {
        b.HasOne(s => s.Customer).WithMany(c => c.Sales).HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(s => s.Items).WithOne(i => i.CreditSale).HasForeignKey(i => i.CreditSaleId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<CreditSaleItem> b)
    {
        b.Property(i => i.Description).IsRequired().HasMaxLength(200);
        b.Property(i => i.Quantity).HasPrecision(18, 3);
    }

    public void Configure(EntityTypeBuilder<CreditPayment> b)
    {
        b.HasOne(p => p.Customer).WithMany(c => c.Payments).HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}

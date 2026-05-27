using System.Linq.Expressions;
using Merkatto.Application.Common;
using Merkatto.Domain.Audit;
using Merkatto.Domain.Auth;
using Merkatto.Domain.Catalog;
using Merkatto.Domain.Common;
using Merkatto.Domain.Credit;
using Merkatto.Domain.Inventory;
using Merkatto.Domain.Operations;
using Merkatto.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>();

    public DbSet<DailyClosing> DailyClosings => Set<DailyClosing>();
    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<CreditCustomer> CreditCustomers => Set<CreditCustomer>();
    public DbSet<CreditSale> CreditSales => Set<CreditSale>();
    public DbSet<CreditSaleItem> CreditSaleItems => Set<CreditSaleItem>();
    public DbSet<CreditPayment> CreditPayments => Set<CreditPayment>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Soft-delete: hide deleted rows automatically for every ISoftDelete entity.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var prop = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var filter = Expression.Lambda(Expression.Not(prop), parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // Money/quantities default to numeric(18,2); specific fields override in their config.
        builder.Properties<decimal>().HavePrecision(18, 2);
        base.ConfigureConventions(builder);
    }
}

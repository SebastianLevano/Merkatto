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
using Merkatto.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<AppSetting>(b =>
        {
            b.HasKey(s => s.Key);
            b.Property(s => s.Key).HasMaxLength(100);
            b.Property(s => s.Value).HasMaxLength(500);
        });

        // jsonb is Postgres-only; SQLite stores these as TEXT (default).
        if (Database.ProviderName?.Contains("Npgsql") == true)
        {
            modelBuilder.Entity<AuditLog>()
                .Property(a => a.OldValues).HasColumnType("jsonb");
            modelBuilder.Entity<AuditLog>()
                .Property(a => a.NewValues).HasColumnType("jsonb");
        }

        // SQLite rejects DateTimeOffset in ORDER BY and WHERE clauses.
        // Map all DateTimeOffset properties to long (UTC ticks) so SQLite can sort/compare them.
        if (Database.IsSqlite())
        {
            var dtoConverter = new ValueConverter<DateTimeOffset, long>(
                v => v.UtcTicks,
                v => new DateTimeOffset(v, TimeSpan.Zero));
            var dtoNullConverter = new ValueConverter<DateTimeOffset?, long?>(
                v => v == null ? null : v.Value.UtcTicks,
                v => v == null ? null : new DateTimeOffset(v.Value, TimeSpan.Zero));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTimeOffset))
                        property.SetValueConverter(dtoConverter);
                    else if (property.ClrType == typeof(DateTimeOffset?))
                        property.SetValueConverter(dtoNullConverter);
                }
            }
        }

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
        // All decimal properties use a fixed-point long converter (×10,000) so that SQL
        // aggregations (SUM, ORDER BY) work identically on both PostgreSQL and SQLite.
        // C# code always sees decimal; the conversion is transparent to the application.
        builder.Properties<decimal>().HaveConversion<FixedPoint4Converter>();
        base.ConfigureConventions(builder);
    }
}

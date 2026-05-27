using Merkatto.Domain.Audit;
using Merkatto.Domain.Auth;
using Merkatto.Domain.Catalog;
using Merkatto.Domain.Common;
using Merkatto.Domain.Credit;
using Merkatto.Domain.Inventory;
using Merkatto.Domain.Operations;
using Merkatto.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Common;

/// <summary>
/// Application-facing view of the persistence layer. Keeps Application decoupled from the
/// concrete EF Core implementation in Infrastructure.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Category> Categories { get; }
    DbSet<Brand> Brands { get; }
    DbSet<Product> Products { get; }

    DbSet<Supplier> Suppliers { get; }
    DbSet<Purchase> Purchases { get; }
    DbSet<PurchaseItem> PurchaseItems { get; }

    DbSet<StockMovement> StockMovements { get; }
    DbSet<InventoryAdjustment> InventoryAdjustments { get; }

    DbSet<DailyClosing> DailyClosings { get; }
    DbSet<Expense> Expenses { get; }

    DbSet<CreditCustomer> CreditCustomers { get; }
    DbSet<CreditSale> CreditSales { get; }
    DbSet<CreditSaleItem> CreditSaleItems { get; }
    DbSet<CreditPayment> CreditPayments { get; }

    DbSet<AuditLog> AuditLogs { get; }

    DbSet<AppSetting> AppSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

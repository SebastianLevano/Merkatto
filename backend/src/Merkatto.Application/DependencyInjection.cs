using FluentValidation;
using Merkatto.Application.Auth;
using Merkatto.Application.Catalog;
using Merkatto.Application.Credit;
using Merkatto.Application.Dashboard;
using Merkatto.Application.Inventory;
using Merkatto.Application.Operations;
using Merkatto.Application.Purchasing;
using Microsoft.Extensions.DependencyInjection;

namespace Merkatto.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
        services.AddScoped<AuthService>();
        services.AddScoped<ProductService>();
        services.AddScoped<CategoryBrandService>();
        services.AddScoped<PurchaseService>();
        services.AddScoped<SupplierService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<ExpenseService>();
        services.AddScoped<DailyClosingService>();
        services.AddScoped<CreditService>();
        services.AddScoped<DashboardService>();
        return services;
    }
}

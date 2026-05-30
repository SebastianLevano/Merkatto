using FluentValidation;
using Merkatto.Application.Alerts;
using Merkatto.Application.Audit;
using Merkatto.Application.Auth;
using Merkatto.Application.Catalog;
using Merkatto.Application.Credit;
using Merkatto.Application.Dashboard;
using Merkatto.Application.Inventory;
using Merkatto.Application.Nrus;
using Merkatto.Application.Operations;
using Merkatto.Application.Purchasing;
using Merkatto.Application.Settings;
using Merkatto.Application.Users;
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
        services.AddScoped<AlertService>();
        services.AddScoped<NrusService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<TimelineService>();
        services.AddScoped<SettingsService>();
        services.AddScoped<UserService>();
        return services;
    }
}

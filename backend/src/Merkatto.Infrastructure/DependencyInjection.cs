using Merkatto.Application.Auth;
using Merkatto.Application.Common;
using Merkatto.Application.Reports;
using Merkatto.Infrastructure.Persistence;
using Merkatto.Infrastructure.Reports;
using Merkatto.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace Merkatto.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        services.AddScoped<AuditingInterceptor>();
        services.AddScoped<IReportService, ReportService>();

        var provider = config.GetValue<string>("Database:Provider") ?? "Postgres";
        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditingInterceptor>();

            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(connectionString)
                       .UseSnakeCaseNamingConvention()
                       .AddInterceptors(interceptor);
            else
                options.UseNpgsql(connectionString)
                       .UseSnakeCaseNamingConvention()
                       .AddInterceptors(interceptor);
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.Configure<SeedSettings>(config.GetSection(SeedSettings.SectionName));
        services.AddSingleton(sp =>
        {
            var s = new SeedSettings();
            config.GetSection(SeedSettings.SectionName).Bind(s);
            return s;
        });
        services.AddScoped<DbInitializer>();

        return services;
    }
}

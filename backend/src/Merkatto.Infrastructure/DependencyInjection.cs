using Merkatto.Application.Auth;
using Merkatto.Application.Common;
using Merkatto.Infrastructure.Persistence;
using Merkatto.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Merkatto.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<AuditingInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(config.GetConnectionString("Default"))
                   .UseSnakeCaseNamingConvention()
                   .AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
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

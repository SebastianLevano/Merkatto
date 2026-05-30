using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Merkatto.Infrastructure.Persistence;

/// <summary>
/// Used by `dotnet ef` at design time. Set EF_PROVIDER=Sqlite to generate SQLite migrations.
/// Defaults to PostgreSQL.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var provider = Environment.GetEnvironmentVariable("EF_PROVIDER") ?? "Postgres";
        var builder = new DbContextOptionsBuilder<AppDbContext>();

        if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var path = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                       ?? "Data Source=merkatto_design.db";
            builder.UseSqlite(path).UseSnakeCaseNamingConvention();
        }
        else
        {
            var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                             ?? "Host=localhost;Port=5432;Database=merkatto;Username=merkatto;Password=merkatto";
            builder.UseNpgsql(connection).UseSnakeCaseNamingConvention();
        }

        return new AppDbContext(builder.Options);
    }
}

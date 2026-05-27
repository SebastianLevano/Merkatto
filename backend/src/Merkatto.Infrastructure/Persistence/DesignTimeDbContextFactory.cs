using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Merkatto.Infrastructure.Persistence;

/// <summary>
/// Used by `dotnet ef` at design time so migrations don't require the API host to start.
/// The connection string is irrelevant for scaffolding migrations.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                         ?? "Host=localhost;Port=5432;Database=merkatto;Username=merkatto;Password=merkatto";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}

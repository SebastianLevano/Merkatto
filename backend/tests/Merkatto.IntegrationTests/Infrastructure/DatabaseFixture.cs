using Merkatto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Merkatto.IntegrationTests.Infrastructure;

// ── Postgres fixture (Testcontainers) ──────────────────────────────────────

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .AddInterceptors(new AuditingInterceptor(new StubCurrentUser(), new StubClock()))
            .Options);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture> { }

// ── SQLite fixture (in-memory, shared connection) ──────────────────────────

public sealed class SqliteFixture : IAsyncLifetime
{
    // Shared connection keeps the in-memory DB alive for the full test run.
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection = new("Data Source=:memory:");

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .AddInterceptors(new AuditingInterceptor(new StubCurrentUser(), new StubClock()))
            .Options);

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await using var ctx = CreateContext();
        // EnsureCreated generates the schema from the current EF model using
        // SQLite-native types (INTEGER for long PKs, etc.) — no migration files needed.
        await ctx.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}

[CollectionDefinition("Sqlite")]
public sealed class SqliteCollection : ICollectionFixture<SqliteFixture> { }

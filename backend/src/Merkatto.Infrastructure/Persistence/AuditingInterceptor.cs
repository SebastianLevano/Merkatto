using System.Text.Json;
using Merkatto.Application.Common;
using Merkatto.Domain.Audit;
using Merkatto.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Merkatto.Infrastructure.Persistence;

/// <summary>
/// On save: stamps auditable fields, turns hard deletes into soft deletes, and writes an
/// <see cref="AuditLog"/> row per change (who/what/when). Refresh tokens and audit logs
/// themselves are not audited to avoid noise/recursion.
/// </summary>
public sealed class AuditingInterceptor(ICurrentUser currentUser, IDateTimeProvider clock) : SaveChangesInterceptor
{
    private static readonly HashSet<string> ExcludedFromLog = ["RefreshToken", "AuditLog"];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context is not null)
            Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void Apply(DbContext context)
    {
        var now = clock.UtcNow;
        var user = currentUser.Email ?? "system";
        var logs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is AuditLog) continue;

            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = now;
                    auditable.CreatedBy = user;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.UpdatedAt = now;
                    auditable.UpdatedBy = user;
                }
            }

            // Hard delete -> soft delete
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDelete soft)
            {
                entry.State = EntityState.Modified;
                soft.IsDeleted = true;
                soft.DeletedAt = now;
            }

            var log = BuildLog(entry, now);
            if (log is not null) logs.Add(log);
        }

        if (logs.Count > 0)
            context.Set<AuditLog>().AddRange(logs);
    }

    private AuditLog? BuildLog(EntityEntry entry, DateTimeOffset now)
    {
        var name = entry.Metadata.ClrType.Name;
        if (ExcludedFromLog.Contains(name)) return null;

        string action;
        var soft = entry.Entity as ISoftDelete;
        if (entry.State == EntityState.Added) action = "Created";
        else if (entry.State == EntityState.Modified) action = soft is { IsDeleted: true } ? "Deleted" : "Updated";
        else return null;

        var id = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id")?.CurrentValue?.ToString();

        return new AuditLog
        {
            Action = action,
            EntityName = name,
            EntityId = id,
            UserId = currentUser.UserId?.ToString(),
            IpAddress = currentUser.IpAddress,
            Timestamp = now,
            OldValues = action == "Created" ? null : Serialize(entry, original: true),
            NewValues = action == "Deleted" ? null : Serialize(entry, original: false)
        };
    }

    private static string Serialize(EntityEntry entry, bool original)
    {
        var values = new Dictionary<string, object?>();
        foreach (var p in entry.Properties)
        {
            if (p.Metadata.Name is "PasswordHash" or "TokenHash") continue; // never log secrets
            values[p.Metadata.Name] = original ? p.OriginalValue : p.CurrentValue;
        }
        return JsonSerializer.Serialize(values);
    }
}

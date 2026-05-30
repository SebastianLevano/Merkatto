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
///
/// Audit log rows are written in a SEPARATE SaveChanges call (after the main one) so they
/// never share EF Core's temporary-key space with the entities being saved — which would
/// cause SQLite AUTOINCREMENT failures when both land the same temp-PK value.
/// </summary>
public sealed class AuditingInterceptor(ICurrentUser currentUser, IDateTimeProvider clock) : SaveChangesInterceptor
{
    private static readonly HashSet<string> ExcludedFromLog = ["RefreshToken", "AuditLog"];

    // Pending logs are collected during SavingChanges and flushed in SavedChanges.
    // List<> is per-interceptor-instance (scoped), so no concurrency issues.
    private List<AuditLog>? _pending;
    private bool _flushing; // guard against re-entrancy during the flush save

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context is not null && !_flushing)
            Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null && !_flushing)
            Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        if (eventData.Context is not null && _pending is { Count: > 0 } && !_flushing)
            await FlushAsync(eventData.Context, ct);
        return await base.SavedChangesAsync(eventData, result, ct);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is not null && _pending is { Count: > 0 } && !_flushing)
            Flush(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    // ── Stamp: runs BEFORE the main save ─────────────────────────────────────

    private void Stamp(DbContext context)
    {
        var now = clock.UtcNow;
        var user = currentUser.Email ?? "system";
        _pending ??= [];

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
            if (log is not null) _pending.Add(log);
        }
    }

    // ── Flush: runs AFTER the main save, entities now have real DB-assigned IDs ─

    private async Task FlushAsync(DbContext context, CancellationToken ct)
    {
        var logs = _pending!;
        _pending = null;
        _flushing = true;
        try
        {
            // Update EntityId with the real DB-assigned key (was a temp value during Stamp).
            foreach (var log in logs)
            {
                if (log.EntityId is not null &&
                    long.TryParse(log.EntityId, out var tempId) && tempId < 0)
                {
                    // Temp id is negative; try to find the entity's real id via the state manager.
                    var entry = context.ChangeTracker.Entries()
                        .FirstOrDefault(e =>
                            e.Metadata.ClrType.Name == log.EntityName &&
                            e.Entity is not AuditLog);
                    if (entry is not null)
                    {
                        var realId = entry.Properties
                            .FirstOrDefault(p => p.Metadata.Name == "Id")?.CurrentValue?.ToString();
                        if (realId is not null) log.EntityId = realId;
                    }
                }
            }

            context.Set<AuditLog>().AddRange(logs);
            await context.SaveChangesAsync(ct);
        }
        finally
        {
            _flushing = false;
        }
    }

    private void Flush(DbContext context)
    {
        var logs = _pending!;
        _pending = null;
        _flushing = true;
        try
        {
            context.Set<AuditLog>().AddRange(logs);
            context.SaveChanges();
        }
        finally
        {
            _flushing = false;
        }
    }

    // ── Log builders ─────────────────────────────────────────────────────────

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
            if (p.Metadata.Name is "PasswordHash" or "TokenHash") continue;
            values[p.Metadata.Name] = original ? p.OriginalValue : p.CurrentValue;
        }
        return JsonSerializer.Serialize(values);
    }
}

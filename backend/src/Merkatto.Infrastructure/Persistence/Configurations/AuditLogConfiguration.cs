using Merkatto.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Merkatto.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.Property(a => a.Action).IsRequired().HasMaxLength(20);
        b.Property(a => a.EntityName).IsRequired().HasMaxLength(120);
        b.Property(a => a.EntityId).HasMaxLength(64);
        b.Property(a => a.OldValues).HasColumnType("jsonb");
        b.Property(a => a.NewValues).HasColumnType("jsonb");
        b.HasIndex(a => a.Timestamp);
        b.HasIndex(a => new { a.EntityName, a.EntityId });
    }
}

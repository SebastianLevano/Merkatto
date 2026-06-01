using Merkatto.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Merkatto.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.Property(u => u.Email).IsRequired().HasMaxLength(256);
        b.HasIndex(u => u.Email).IsUnique();
        b.Property(u => u.FullName).IsRequired().HasMaxLength(160);
        b.Property(u => u.PasswordHash).IsRequired();
        b.Property(u => u.Role).HasConversion<int>();
        b.Property(u => u.BusinessName).HasMaxLength(120);

        b.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(128);
        b.HasIndex(rt => rt.TokenHash);
        b.Ignore(rt => rt.IsActive);
    }
}

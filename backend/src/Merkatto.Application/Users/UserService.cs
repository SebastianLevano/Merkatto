using Merkatto.Application.Common;
using Merkatto.Domain.Auth;
using Microsoft.EntityFrameworkCore;

namespace Merkatto.Application.Users;

/// <summary>
/// Administrator-only management of the users within this installation. Each install is
/// independent (single-tenant), so this is the only place new admins/collaborators are created.
/// Includes anti-lockout guards so an admin cannot leave the install without an active admin.
/// </summary>
public sealed class UserService(
    IAppDbContext db,
    IPasswordHasher hasher,
    IDateTimeProvider clock,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<UserListItem>> ListAsync(CancellationToken ct) =>
        await db.Users
            .OrderByDescending(u => u.Role == Role.Administrator)
            .ThenBy(u => u.FullName)
            .Select(u => new UserListItem(
                u.Id, u.Email, u.FullName, u.Role, u.IsActive, u.MustChangePassword, u.LastLoginAt, u.BusinessName))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(CreateUserRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLower();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("Ya existe un usuario con ese correo.");

        var user = new User
        {
            Email = email,
            FullName = req.FullName.Trim(),
            PasswordHash = hasher.Hash(req.Password),
            Role = req.Role,
            IsActive = true,
            MustChangePassword = true,
            BusinessName = req.Role == Role.Encargado ? req.BusinessName?.Trim() : null,
            CreatedAt = clock.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user.Id;
    }

    public async Task UpdateAsync(long id, UpdateUserRequest req, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("Usuario no encontrado.");

        var losesAdmin = user.Role == Role.Administrator &&
                         (req.Role != Role.Administrator || !req.IsActive);
        if (losesAdmin)
            await EnsureNotLastAdminAsync(user.Id, ct);

        if (user.Id == currentUser.UserId && !req.IsActive)
            throw new BusinessRuleException("No podés desactivar tu propio usuario.");

        user.FullName = req.FullName.Trim();
        user.Role = req.Role;
        user.IsActive = req.IsActive;
        user.BusinessName = req.Role == Role.Encargado ? req.BusinessName?.Trim() : null;
        await db.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(long id, ResetPasswordRequest req, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("Usuario no encontrado.");

        user.PasswordHash = hasher.Hash(req.Password);
        user.MustChangePassword = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeactivateAsync(long id, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("Usuario no encontrado.");

        if (user.Id == currentUser.UserId)
            throw new BusinessRuleException("No podés desactivar tu propio usuario.");

        if (user.Role == Role.Administrator)
            await EnsureNotLastAdminAsync(user.Id, ct);

        user.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureNotLastAdminAsync(long excludingUserId, CancellationToken ct)
    {
        var otherActiveAdmins = await db.Users.CountAsync(
            u => u.Id != excludingUserId && u.Role == Role.Administrator && u.IsActive, ct);
        if (otherActiveAdmins == 0)
            throw new BusinessRuleException("Debe quedar al menos un administrador activo.");
    }
}

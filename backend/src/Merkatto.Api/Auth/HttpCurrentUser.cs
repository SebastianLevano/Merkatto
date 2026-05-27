using System.Security.Claims;
using Merkatto.Application.Common;

namespace Merkatto.Api.Auth;

/// <summary>Resolves the current user from the HTTP context (JWT claims).</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public long? UserId =>
        long.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? Principal?.FindFirstValue("sub"), out var id)
            ? id
            : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email)
                            ?? Principal?.FindFirstValue("email");

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
}

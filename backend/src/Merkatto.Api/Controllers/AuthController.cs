using Merkatto.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Merkatto.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    private const string RefreshCookie = "merkatto_refresh";

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResult>> Login(LoginRequest request, CancellationToken ct)
    {
        var pair = await auth.LoginAsync(request, ct);
        return Ok(BuildResult(pair));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResult>> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookie, out var raw) || string.IsNullOrEmpty(raw))
            return Unauthorized();

        var pair = await auth.RefreshAsync(raw, ct);
        return Ok(BuildResult(pair));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (Request.Cookies.TryGetValue(RefreshCookie, out var raw) && !string.IsNullOrEmpty(raw))
            await auth.LogoutAsync(raw, ct);

        Response.Cookies.Delete(RefreshCookie);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var user = await auth.GetCurrentAsync(ct);
        return user is null ? Unauthorized() : Ok(user);
    }

    private AuthResult BuildResult(TokenPair pair)
    {
        Response.Cookies.Append(RefreshCookie, pair.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = pair.RefreshTokenExpiresAt,
            Path = "/api/v1/auth"
        });

        // Access token returned in body (kept in memory by the SPA); refresh stays in the cookie.
        return new AuthResult(pair.AccessToken, pair.AccessTokenExpiresAt, pair.RefreshToken, pair.User);
    }
}

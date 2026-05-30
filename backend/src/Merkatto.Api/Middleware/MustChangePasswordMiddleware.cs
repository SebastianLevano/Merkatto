using System.Text.Json;

namespace Merkatto.Api.Middleware;

/// <summary>
/// Defense-in-depth: while an authenticated user carries the <c>must_change_password</c> claim,
/// every endpoint is blocked (403) except the auth endpoints (login/refresh/logout/me/change-password).
/// The claim is cleared on the next token issuance after the password is changed, so the SPA
/// refreshes its token after a successful change to regain access.
/// </summary>
public sealed class MustChangePasswordMiddleware(RequestDelegate next)
{
    private const string AuthPathPrefix = "/api/v1/auth";

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true &&
            user.FindFirst("must_change_password")?.Value == "true" &&
            !context.Request.Path.StartsWithSegments(AuthPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                status = 403,
                title = "Cambio de contraseña requerido",
                detail = "Debés cambiar tu contraseña antes de usar el sistema."
            }));
            return;
        }

        await next(context);
    }
}

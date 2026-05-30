namespace Merkatto.Api.Middleware;

/// <summary>Adds baseline security headers to every response. (HTTPS/HSTS handled at the proxy.)</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["X-XSS-Protection"] = "0";

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            // Pure JSON API: allow nothing.
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        }
        else
        {
            // SPA: same-origin scripts/styles/fonts/images + no framing.
            // 'unsafe-inline' on script-src covers Angular's <link onload="this.media='all'">
            // pattern (async CSS loading emitted by the Angular CLI build optimizer).
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; " +
                "font-src 'self'; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none'";
        }

        await next(context);
    }
}

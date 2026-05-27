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
        // API serves JSON only; lock down what a response may load.
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        await next(context);
    }
}

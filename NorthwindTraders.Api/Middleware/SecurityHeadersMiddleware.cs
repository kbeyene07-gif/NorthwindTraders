namespace NorthwindTraders.Api.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    public async Task Invoke(HttpContext context)
    {
        // Clickjacking protection
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // Stops MIME sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Limits referrer data
        context.Response.Headers["Referrer-Policy"] = "no-referrer";

        // Basic cross-origin controls (adjust as needed)
        context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
        context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";

        // Disable old XSS header (modern browsers use CSP; still ok to set)
        context.Response.Headers["X-XSS-Protection"] = "0";

        // HSTS (only in non-dev)
        if (!env.IsDevelopment())
        {
            // 180 days
            context.Response.Headers["Strict-Transport-Security"] = "max-age=15552000; includeSubDomains";
        }

        if (!context.Request.Path.StartsWithSegments("/swagger"))
        {
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
        }

        await next(context);
    }
}

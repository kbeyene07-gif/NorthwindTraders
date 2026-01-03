using System.Diagnostics;

namespace NorthwindTraders.Api.Middleware;

public sealed class RequestLoggingScopeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingScopeMiddleware> _logger;

    public RequestLoggingScopeMiddleware(RequestDelegate next, ILogger<RequestLoggingScopeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var cid)
            ? cid?.ToString()
            : null;

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["traceId"] = traceId,
            ["correlationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }
}

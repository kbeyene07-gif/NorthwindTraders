using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace NorthwindTraders.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, IHostEnvironment env)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected / request aborted. Don't treat as server error.
            Log.Information("Request aborted by client. Path={Path}", context.Request.Path.Value);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                Log.Error(ex, "Unhandled exception after response started. Path={Path}", context.Request.Path.Value);
                throw;
            }

            var traceId = context.TraceIdentifier;

            var correlationId =
                context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var cidObj) &&
                cidObj is string cid &&
                !string.IsNullOrWhiteSpace(cid)
                    ? cid
                    : null;

            var (statusCode, title, type) = MapException(ex);

            // Log level based on severity
            if (statusCode >= 500)
            {
                Log.Error(ex,
                    "Unhandled exception. Status={Status} TraceId={TraceId} CorrelationId={CorrelationId} Path={Path}",
                    statusCode, traceId, correlationId, context.Request.Path.Value);
            }
            else
            {
                Log.Warning(ex,
                    "Handled exception. Status={Status} TraceId={TraceId} CorrelationId={CorrelationId} Path={Path}",
                    statusCode, traceId, correlationId, context.Request.Path.Value);
            }

            await WriteProblemDetailsAsync(context, ex, statusCode, title, type, traceId, correlationId, env);
        }
    }

    private static (int StatusCode, string Title, string Type) MapException(Exception ex)
    {
        // NOTE: You can replace these with your own custom exceptions later.
        return ex switch
        {
            ArgumentException or FormatException
                => ((int)HttpStatusCode.BadRequest, "Bad request.", "https://httpstatuses.com/400"),

            KeyNotFoundException
                => ((int)HttpStatusCode.NotFound, "Resource not found.", "https://httpstatuses.com/404"),

            UnauthorizedAccessException
                => ((int)HttpStatusCode.Unauthorized, "Unauthorized.", "https://httpstatuses.com/401"),

            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.", "https://httpstatuses.com/500")
        };
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        Exception exception,
        int statusCode,
        string title,
        string type,
        string traceId,
        string? correlationId,
        IHostEnvironment env)
    {
        // In Production: don't leak stack traces or internal messages.
        // In Development: include exception message to speed up debugging.
        var detail = env.IsDevelopment()
            ? exception.Message
            : "Please contact support with the provided correlationId.";

        var problemDetails = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;

        if (!string.IsNullOrWhiteSpace(correlationId))
            problemDetails.Extensions["correlationId"] = correlationId;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
    }
}

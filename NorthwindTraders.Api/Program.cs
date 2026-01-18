using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Identity.Web; // ADD THIS
using Microsoft.OpenApi.Models;
using NorthwindTraders.Api.Middleware;
using NorthwindTraders.Api.Security;
using NorthwindTraders.Application.Common;
using NorthwindTraders.Application.Mapping;
using NorthwindTraders.Application.Services.Customers;
using NorthwindTraders.Application.Services.Orders;
using NorthwindTraders.Application.Services.Products;
using NorthwindTraders.Application.Services.Suppliers;
using NorthwindTraders.Application.Services.OrderItems;
using NorthwindTraders.Infrastructure;
using Serilog;

namespace NorthwindTraders.Api;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Why: configure Serilog early so startup failures and host logs are captured consistently.
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Host.UseSerilog();

        try
        {
            // 1) Controllers + JSON options
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                });

            // Why: enables {version:apiVersion} route constraint used by controllers
            // e.g. [Route("api/v{version:apiVersion}/[controller]")]
            builder.Services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });

            // Why: Ensures FluentValidation/DataAnnotations return a consistent RFC7807 payload
            // (ValidationProblemDetails) instead of different shapes depending on where validation happens.
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problemDetails = new ValidationProblemDetails(context.ModelState)
                    {
                        Title = "Validation failed",
                        Status = StatusCodes.Status400BadRequest,
                        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                        Instance = context.HttpContext.Request.Path
                    };

                    // Why: traceId helps correlate client errors to server logs and distributed tracing.
                    problemDetails.Extensions["traceId"] =
                        Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

                    // Why: MUST match CorrelationIdMiddleware storage key (HeaderName = "X-Correlation-Id").
                    if (context.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var cidObj) &&
                        cidObj is string cid && !string.IsNullOrWhiteSpace(cid))
                    {
                        problemDetails.Extensions["correlationId"] = cid;
                    }

                    return new BadRequestObjectResult(problemDetails)
                    {
                        ContentTypes = { "application/problem+json" }
                    };
                };
            });

            // Why: ProblemDetails gives a consistent error contract across the API (RFC 7807),
            // and we attach traceId/correlationId so issues are supportable.
            builder.Services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = ctx =>
                {
                    ctx.ProblemDetails.Extensions["traceId"] =
                        Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;

                    if (ctx.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var cidObj) &&
                        cidObj is string cid && !string.IsNullOrWhiteSpace(cid))
                    {
                        ctx.ProblemDetails.Extensions["correlationId"] = cid;
                    }
                };
            });

            // 2) Swagger + JWT support (Dev only in pipeline)
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "NorthwindTraders API",
                    Version = "v1"
                });

                // XML comments
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter: Bearer {JWT token}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // 3) CORS
            var corsPolicyName = "DefaultCors";

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(corsPolicyName, policy =>
                {
                    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

                    if (origins is { Length: > 0 })
                    {
                        policy.WithOrigins(origins)
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    }
                    else
                    {
                        // Dev fallback (NOT for prod)
                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    }
                });
            });

            // 4) Rate limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var key = httpContext.User?.Identity?.IsAuthenticated == true
                        ? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "auth"
                        : httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon";

                    return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
                });

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            // 5) Infrastructure: DbContext only when NOT Testing
            // Why: prevents tests from accidentally registering two DbContexts (POST writes DB A, GET reads DB B).
            if (!builder.Environment.IsEnvironment("Testing"))
            {
                var cs = builder.Configuration.GetConnectionString("DefaultConnection");

                if (string.IsNullOrWhiteSpace(cs))
                    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

                builder.Services.AddDbContext<NorthwindTradersContext>(options =>
                    options.UseSqlServer(cs));
            }
            builder.Services.AddScoped<INorthwindDbContext>(sp => sp.GetRequiredService<NorthwindTradersContext>());

            // 6) App services
            builder.Services.AddAutoMapper(typeof(AppMappingProfile));
            builder.Services.AddScoped<ICustomerService, CustomerService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<ISupplierService, SupplierService>();
            builder.Services.AddScoped<IOrderItemService, OrderItemService>();

            // 7) Authorization policies 
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthScopes.CustomersRead, policy => policy.RequireAuthenticatedUser());
                options.AddPolicy(AuthScopes.CustomersWrite, policy => policy.RequireAuthenticatedUser());
            });

            //  8) Authentication: Azure Entra ID (Microsoft Identity Platform)
            // Reads settings from: "AzureAd" section in appsettings*.json
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

            // 9) Health checks
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "ready" });

            var app = builder.Build();

            // Force-create Logs folder + write a startup log
            var logsPath = Path.Combine(app.Environment.ContentRootPath, "Logs");
            Directory.CreateDirectory(logsPath);
            Log.Information("Logs folder path: {LogsPath}", logsPath);

            // =========================
            // Middleware pipeline
            // =========================

            // Why: CorrelationId first so EVERY response/error can include it (headers + ProblemDetails + logs).
            app.UseMiddleware<CorrelationIdMiddleware>();

            // Why: Endpoint routing must run before auth so auth can evaluate endpoint metadata/policies correctly.
            app.UseRouting();

            app.UseCors(corsPolicyName);

            app.UseAuthentication();
            app.UseAuthorization();

            // Why: Security headers on every response (OK to be after auth).
            app.UseMiddleware<SecurityHeadersMiddleware>();

            // Why: Single centralized exception handler that guarantees RFC7807 ProblemDetails for unhandled exceptions.
            // IMPORTANT: keep only ONE exception middleware (GlobalExceptionMiddleware).
            app.UseMiddleware<GlobalExceptionMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Why: request logs enriched with traceId + correlationId for production-grade observability.
            app.UseSerilogRequestLogging(opts =>
            {
                opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
                {
                    diagCtx.Set("traceId", Activity.Current?.Id ?? httpCtx.TraceIdentifier);

                    if (httpCtx.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var cidObj) &&
                        cidObj is string cid && !string.IsNullOrWhiteSpace(cid))
                    {
                        diagCtx.Set("correlationId", cid);
                    }
                };
            });

            app.UseRateLimiter();

            app.MapControllers();
            app.MapHealthChecks("/health");

            app.Run();
        }
        catch (Exception ex)
        {
            // Why: don’t swallow startup failures; tests/CI need the real exception.
            Log.Fatal(ex, "Application start-up failed");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

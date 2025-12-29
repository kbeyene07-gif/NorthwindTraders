using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NorthwindTraders.Api.Middleware;
using NorthwindTraders.Api.Security;
using NorthwindTraders.Application.Mapping;
using NorthwindTraders.Application.Services.Customers;
using NorthwindTraders.Application.Services.Orders;
using NorthwindTraders.Application.Services.Products;
using NorthwindTraders.Application.Services.Suppliers;
using NorthwindTraders.Infrastructure;
using Serilog;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Serilog: configure as early as possible
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

            // 2) Swagger + JWT support (Dev only in pipeline)
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "NorthwindTraders API",
                    Version = "v1"
                });

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
                    Description = "Enter: Bearer {your JWT token}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // 3) API Versioning
            builder.Services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });

            // 4) Auth settings
            var authSection = builder.Configuration.GetSection("Authentication");
            var authority = authSection["Authority"];
            var audience = authSection["Audience"];

            // 5) Authentication: JWT Bearer
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authority;
                    options.Audience = audience;

                    // Senior-grade: strict validation (defaults usually true, but make explicit)
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        RoleClaimType = "roles",
                        NameClaimType = ClaimTypes.NameIdentifier,
                        ClockSkew = TimeSpan.FromMinutes(2)
                    };
                });

            // 6) Authorization policies
            builder.Services.AddAuthorization(AuthorizationPolicies.AddAppPolicies);

            // 7) AutoMapper
            builder.Services.AddAutoMapper(typeof(AppMappingProfile).Assembly);

            // 8) DbContext
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<NorthwindTradersContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddScoped<NorthwindTraders.Application.Common.INorthwindDbContext>(sp =>
                sp.GetRequiredService<NorthwindTradersContext>());

            // 9) Dependency Injection for services
            builder.Services.AddScoped<ICustomerService, CustomerService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<ISupplierService, SupplierService>();
            builder.Services.AddScoped<IOrderService, OrderService>();

            // 10) CORS
            var corsPolicyName = "AngularClient";
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(corsPolicyName, policy =>
                {
                    policy
                        .WithOrigins("http://localhost:4200", "https://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // 11) Health Checks (readiness)

            builder.Services.AddHealthChecks()
    .AddDbContextCheck<NorthwindTradersContext>(
        name: "sqlserver",
        failureStatus: HealthStatus.Unhealthy);


            // 12) Rate Limiting + 429 ProblemDetails
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, token) =>
                {
                    var httpContext = context.HttpContext;

                    httpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var cidObj);

                    var problem = new ProblemDetails
                    {
                        Type = "https://httpstatuses.com/429",
                        Title = "Too many requests.",
                        Status = StatusCodes.Status429TooManyRequests,
                        Detail = "Rate limit exceeded. Try again soon.",
                        Instance = httpContext.Request.Path
                    };

                    problem.Extensions["traceId"] = httpContext.TraceIdentifier;

                    if (cidObj is string cid && !string.IsNullOrWhiteSpace(cid))
                        problem.Extensions["correlationId"] = cid;

                    httpContext.Response.ContentType = "application/problem+json";
                    await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: token);
                };

                // Global limiter: protect everything
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var userKey = httpContext.User?.Identity?.IsAuthenticated == true
                        ? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "auth-unknown"
                        : null;

                    var ipKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "ip-unknown";
                    var key = userKey ?? ipKey;

                    return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
                });

                // Named policy: stricter for auth endpoints (optional)
                options.AddPolicy("auth", httpContext =>
                {
                    var ipKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "ip-unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(ipKey, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
                });
            });

            var app = builder.Build();

            // Force-create Logs folder + write a startup log
            var logsPath = Path.Combine(app.Environment.ContentRootPath, "Logs");
            Directory.CreateDirectory(logsPath);
            Log.Information("Logs folder path: {LogsPath}", logsPath);

            // Middleware pipeline (order matters)
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseMiddleware<CorrelationIdMiddleware>();

            app.UseMiddleware<SecurityHeadersMiddleware>();

            app.UseHttpsRedirection();

            // Logs HTTP requests (method/path/status/duration)
            app.UseSerilogRequestLogging();

            // Global exception handling
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseCors(corsPolicyName);

            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            // Endpoints
            app.MapControllers();
            app.MapHealthChecks("/health");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application start-up failed");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

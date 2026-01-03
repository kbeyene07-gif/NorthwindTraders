using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NorthwindTraders.Api.Middleware;
using NorthwindTraders.Api.Security;
using NorthwindTraders.Application;
using NorthwindTraders.Application.Mapping;
using NorthwindTraders.Application.Services.Customers;
using NorthwindTraders.Application.Services.Orders;
using NorthwindTraders.Application.Services.Products;
using NorthwindTraders.Application.Services.Suppliers;
using NorthwindTraders.Infrastructure;
using Serilog;

namespace NorthwindTraders.Api;
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
            builder.Services
           .AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null);

            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssembly(typeof(AppMappingProfile).Assembly);


            // ✅ ProblemDetails (RFC 7807) + traceId/correlationId everywhere
            builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problemDetails = new ValidationProblemDetails(context.ModelState)
                    {
                        Title = "Validation failed",
                        Status = StatusCodes.Status400BadRequest,
                        Type = "https://httpstatuses.com/400",
                        Instance = context.HttpContext.Request.Path
                    };

                    // correlation id 
                    if (context.HttpContext.Items.TryGetValue("CorrelationId", out var cid) && cid is string correlationId)
                        problemDetails.Extensions["correlationId"] = correlationId;

                    return new BadRequestObjectResult(problemDetails)
                    {
                        ContentTypes = { "application/problem+json" }
                    };
                };
            });

            builder.Services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = ctx =>
                {
                    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;

                    // correlation id comes from  CorrelationIdMiddleware
                    ctx.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var cidObj);
                    if (cidObj is string cid && !string.IsNullOrWhiteSpace(cid))
                        ctx.ProblemDetails.Extensions["correlationId"] = cid;
                };
            });

            // ✅ Middleware DI registrations (needed for UseMiddleware<T>())
            builder.Services.AddTransient<ExceptionHandlingMiddleware>();
            builder.Services.AddTransient<CorrelationIdMiddleware>();
            builder.Services.AddTransient<SecurityHeadersMiddleware>();

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
                    Description = "Enter: Bearer {JWT token}"
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
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


            if (string.IsNullOrWhiteSpace(connectionString) && !builder.Environment.IsEnvironment("Testing"))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }
            if (!builder.Environment.IsEnvironment("Testing"))
                builder.Services.AddDbContext<NorthwindTradersContext>(options =>options.UseSqlServer(connectionString));

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

            // ✅ Middleware pipeline (order matters)

            // 1) Correlation + security headers first (so every response has them)
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<SecurityHeadersMiddleware>();

            // 2) Global exception handling EARLY (wraps everything below)
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            // 3) Swagger (dev only)
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Logs HTTP requests (method/path/status/duration)
            app.UseSerilogRequestLogging();

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
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

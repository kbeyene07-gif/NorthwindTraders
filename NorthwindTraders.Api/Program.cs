using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NorthwindTraders.Api.Middleware;
using NorthwindTraders.Api.Security;
using NorthwindTraders.Api.Services.Orders;
using NorthwindTraders.Application.Mapping;
using NorthwindTraders.Application.Services.Customers;
using NorthwindTraders.Application.Services.Products;
using NorthwindTraders.Application.Services.Suppliers;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using NorthwindTraders.Application.Common;
using NorthwindTraders.Application;
using NorthwindTraders.Infrastructure;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1️ Controllers + JSON options
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

        // 2️ Swagger + JWT support
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "NorthwindTraders API",
                Version = "v1"
            });

            // ✅ XML comments for better Swagger docs
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

            // JWT Bearer definition
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter: Bearer {your JWT token}"
            });

            // Require JWT for all endpoints (unless overridden)
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


        // 3 Add API Versioning here
        builder.Services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ReportApiVersions = true;
        });


        // 4 Auth0 / Authentication settings from appsettings.json
        var authSection = builder.Configuration.GetSection("Authentication");
        var authority = authSection["Authority"];
        var audience = authSection["Audience"];

        // 5 Authentication: JWT Bearer
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;  // 🔐 Who issued the token (Auth0 domain)
                options.Audience = audience;   // 🎯 Who the token is for (your API identifier)

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Auth0 typically puts roles in a "roles" claim
                    RoleClaimType = "roles",

                    // Optional: what claim to treat as the "Name"
                    NameClaimType = ClaimTypes.NameIdentifier
                };
            });

        // 6 Authorization: scopes + roles + mixed policies
        builder.Services.AddAuthorization(AuthorizationPolicies.AddAppPolicies);

        // 7 Register AutoMapper

        builder.Services.AddAutoMapper(typeof(AppMappingProfile).Assembly);


        // 8 DbContext
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
      ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        builder.Services.AddDbContext<NorthwindTraders.Infrastructure.NorthwindTradersContext>(options =>
     options.UseSqlServer(connectionString));

        builder.Services.AddScoped<NorthwindTraders.Application.Common.INorthwindDbContext>(sp =>
    sp.GetRequiredService<NorthwindTraders.Infrastructure.NorthwindTradersContext>());



        //9 ️ Dependency Injection for Services

        builder.Services.AddScoped<ICustomerService, CustomerService>();
        builder.Services.AddScoped<IProductService, ProductService>();
        builder.Services.AddScoped<ISupplierService, SupplierService>();
        builder.Services.AddScoped<IOrderService, OrderService>();


        var app = builder.Build();

        //10 Global exception handling middleware
        app.UseMiddleware<ExceptionHandlingMiddleware>();


        //  Middleware pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        // 🔐 Order matters: auth → then authorization
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
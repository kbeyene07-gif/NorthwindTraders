using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NorthwindTraders.Api;
using NorthwindTraders.Infrastructure;
using NorthwindTraders.Tests.Integration.TestAuth;

namespace NorthwindTraders.Tests.Integration.TestHost;

public sealed class NorthwindApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 1) Replace real DbContext with InMemory
            services.RemoveAll(typeof(DbContextOptions<NorthwindTradersContext>));

            services.AddDbContext<NorthwindTradersContext>(options =>
                options.UseInMemoryDatabase("Northwind_IntegrationTests_" + Guid.NewGuid()));

            // 2) Plug in test authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });

            // 3) Ensure DB is created for each test host
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NorthwindTradersContext>();
            db.Database.EnsureCreated();
        });
    }
}

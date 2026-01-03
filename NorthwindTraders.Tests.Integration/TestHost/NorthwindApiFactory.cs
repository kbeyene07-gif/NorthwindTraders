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
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // ✅ Remove ANY existing DbContext registration coming from Program.cs
            services.RemoveAll(typeof(DbContextOptions<NorthwindTradersContext>));
            services.RemoveAll(typeof(NorthwindTradersContext));

            // ✅ Register ONE shared in-memory database for the whole test host
            services.AddDbContext<NorthwindTradersContext>(options =>
                options.UseInMemoryDatabase("Northwind_IntegrationTests"));

            // ✅ Test authentication scheme (ONLY here, not in Program.cs)
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });
        });
    }
}

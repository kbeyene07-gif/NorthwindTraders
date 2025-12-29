using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NorthwindTraders.Infrastructure;

public class NorthwindTradersDbContextFactory : IDesignTimeDbContextFactory<NorthwindTradersContext>
{
    public NorthwindTradersContext CreateDbContext(string[] args)
    {
        // Load config from API project appsettings.json
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "NorthwindTraders.Api");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<NorthwindTradersContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new NorthwindTradersContext(optionsBuilder.Options);
    }
}

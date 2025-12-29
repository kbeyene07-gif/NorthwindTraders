using Microsoft.EntityFrameworkCore;
using NorthwindTraders.Infrastructure;
using System;

namespace NorthwindTraders.Tests.Unit.TestHelpers;

public static class DbContextTestFactory
{
    public static NorthwindTradersContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<NorthwindTradersContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        return new NorthwindTradersContext(options);
    }
}

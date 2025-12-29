using NorthwindTraders.Tests.Integration;
using NorthwindTraders.Tests.Integration.TestHost;
using Xunit;

public class ProductsCatalogTests : IClassFixture<MsSqlFixture>
{
    private readonly HttpClient _client;

    public ProductsCatalogTests(MsSqlFixture fixture)
    {
        var factory = new CustomWebApplicationFactory(fixture.ConnectionString);
        _client = factory.CreateClient();
    }
}

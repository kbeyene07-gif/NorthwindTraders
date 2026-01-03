using NorthwindTraders.Tests.Integration;
using Xunit;

public class ProductsCatalogTests : IClassFixture<MsSqlFixture>
{
    private readonly HttpClient _client;

    public ProductsCatalogTests(MsSqlFixture fixture)
    {
        _client = fixture.Client;
    }
}

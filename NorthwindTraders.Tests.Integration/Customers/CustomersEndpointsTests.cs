using FluentAssertions;
using NorthwindTraders.Application.Dtos.Customers;
using NorthwindTraders.Tests.Integration.TestAuth;
using NorthwindTraders.Tests.Integration.TestHost;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace NorthwindTraders.Tests.Integration.Customers;

public class CustomersEndpointsTests : IClassFixture<NorthwindApiFactory>
{
    private readonly HttpClient _client;

    public CustomersEndpointsTests(NorthwindApiFactory factory)
    {
        _client = factory.CreateClient();
        // Mark requests as authenticated (per our TestAuthHandler)
        _client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "true");
    }

    [Fact]
    public async Task POST_Customers_ShouldCreate_Then_GET_ShouldReturn()
    {
        // NOTE: adjust route if yours differs
        var createUrl = "/api/v1/customers";

        var createDto = new CreateCustomerDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            City = "New York",
            Country = "USA",
            Address1 = "10 Broadway",
            Address2 = null,
            State = "NY",
            ZipCode = "10001",
            Phone = "555-1234"
        };

        var post = await _client.PostAsJsonAsync(createUrl, createDto);
        post.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        // If your API returns CustomerDto in body:
        var created = await post.Content.ReadFromJsonAsync<CustomerDto>();
        created.Should().NotBeNull();
        created!.Id.Should().BeGreaterThan(0);

        var getUrl = $"/api/v1/customers/{created.Id}";
        var get = await _client.GetAsync(getUrl);
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await get.Content.ReadFromJsonAsync<CustomerDto>();
        fetched.Should().NotBeNull();
        fetched!.FirstName.Should().Be("Jane");
        fetched.LastName.Should().Be("Smith");
    }

    [Fact]
    public async Task GET_Customers_UnknownId_ShouldReturn404()
    {
        var res = await _client.GetAsync("/api/v1/customers/999999");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

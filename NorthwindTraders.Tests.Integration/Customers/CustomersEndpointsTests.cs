using FluentAssertions;
using NorthwindTraders.Application.Dtos.Customers;
using NorthwindTraders.Tests.Integration.TestHost;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace NorthwindTraders.Tests.Integration.Customers;

public class CustomersEndpointsTests : IClassFixture<NorthwindApiFactory>
{
    private readonly NorthwindApiFactory _factory;

    public CustomersEndpointsTests(NorthwindApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthedClient()
    {
        var client = _factory.CreateClient();

        // If TestAuthHandler expects this header, keep it.
        // If it doesn’t, it won’t hurt.
        if (!client.DefaultRequestHeaders.Contains("X-Test-Auth"))
            client.DefaultRequestHeaders.Add("X-Test-Auth", "true");

        return client;
    }

    [Fact]
    public async Task GET_Customers_UnknownId_ShouldReturn404()
    {
        using var client = CreateAuthedClient();

        var res = await client.GetAsync("/api/v1/customers/999999");

        if (res.StatusCode != HttpStatusCode.NotFound)
        {
            var body = await res.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"Expected 404 NotFound but got {(int)res.StatusCode} {res.StatusCode}. Body: {body}");
        }

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Customers_ShouldCreate_Then_GET_ShouldReturn()
    {
        using var client = CreateAuthedClient();

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

        var post = await client.PostAsJsonAsync(createUrl, createDto);

        if (post.StatusCode != HttpStatusCode.Created && post.StatusCode != HttpStatusCode.OK)
        {
            var postBody = await post.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"POST expected 200/201 but got {(int)post.StatusCode} {post.StatusCode}. Body: {postBody}");
        }

        // Read body (optional, depending on API contract)
        var created = await post.Content.ReadFromJsonAsync<CustomerDto>();
        created.Should().NotBeNull("POST should return a CustomerDto in the response body");
        created!.Id.Should().BeGreaterThan(0);

        // Prefer the Location header for the follow-up GET (most accurate)
        // If Location is missing, fall back to building the URL.
        var location = post.Headers.Location?.ToString();
        var getUrl = !string.IsNullOrWhiteSpace(location)
            ? location
            : $"/api/v1/customers/{created.Id}";

        // Some servers return absolute URLs; HttpClient can handle relative only unless BaseAddress is set.
        // Normalize to relative if needed:
        if (Uri.TryCreate(getUrl, UriKind.Absolute, out var absolute))
            getUrl = absolute.PathAndQuery;

        var get = await client.GetAsync(getUrl);

        if (get.StatusCode != HttpStatusCode.OK)
        {
            var getBody = await get.Content.ReadAsStringAsync();
            var postBody = await post.Content.ReadAsStringAsync();

            throw new Xunit.Sdk.XunitException(
                $"GET expected 200 but got {(int)get.StatusCode} {get.StatusCode}.\n" +
                $"GET URL: {getUrl}\n" +
                $"POST Location: {location}\n" +
                $"POST Body: {postBody}\n" +
                $"GET Body: {getBody}");
        }

        var fetched = await get.Content.ReadFromJsonAsync<CustomerDto>();
        fetched.Should().NotBeNull();
        fetched!.FirstName.Should().Be("Jane");
        fetched.LastName.Should().Be("Smith");
    }
}

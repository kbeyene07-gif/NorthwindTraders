using NorthwindTraders.Tests.Integration.TestHost;
using Testcontainers.MsSql;
using Xunit;

namespace NorthwindTraders.Tests.Integration;

public sealed class MsSqlFixture : IAsyncLifetime
{

    public string ConnectionString { get; private set; } = default!;

    public CustomWebApplicationFactory Factory { get; private set; } = default!;
    public HttpClient Client { get; private set; } = default!;

    public async Task InitializeAsync()
    {

        Factory = new CustomWebApplicationFactory(ConnectionString);
        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();

        await Task.CompletedTask;
    }

}

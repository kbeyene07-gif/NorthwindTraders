using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;
using Xunit;

namespace NorthwindTraders.Tests.Integration;

public sealed class MsSqlFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _db = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _db.GetConnectionString();

    public async Task InitializeAsync() => await _db.StartAsync();
    public async Task DisposeAsync() => await _db.DisposeAsync();
}

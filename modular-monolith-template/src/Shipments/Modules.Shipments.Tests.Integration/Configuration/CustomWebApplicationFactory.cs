using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ModularMonolith.Host;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace Modules.Shipments.Tests.Integration.Configuration;

public class CustomWebApplicationFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
		    .WithImage("postgres:latest")
		    .WithDatabase("test")
		    .WithUsername("admin")
		    .WithPassword("admin")
		    .Build();

    private DbConnection _dbConnection = null!;
    private Respawner _respawner = null!;

    public HttpClient HttpClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
	    await _dbContainer.StartAsync();

	    _dbConnection = new NpgsqlConnection(_dbContainer.GetConnectionString());

	    HttpClient = CreateClient();

	    await _dbConnection.OpenAsync();
	    await InitializeRespawnerAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await _dbConnection.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
	    await _respawner.ResetAsync(_dbConnection);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
	    builder.UseSetting("ConnectionStrings:Postgres", _dbContainer.GetConnectionString());
    }

    private async Task InitializeRespawnerAsync()
    {
	    _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
	    {
		    SchemasToInclude = [ "stocks", "carriers", "shipments" ],
		    DbAdapter = DbAdapter.Postgres
	    });
    }
}

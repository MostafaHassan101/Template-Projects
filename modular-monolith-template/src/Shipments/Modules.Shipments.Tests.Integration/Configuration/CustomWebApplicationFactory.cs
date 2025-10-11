using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ModularMonolith.Host;
using Microsoft.Data.SqlClient;
using Respawn;
using Testcontainers.MsSql;

namespace Modules.Shipments.Tests.Integration.Configuration;

public class CustomWebApplicationFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
		    .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
		    .WithPassword("YourStrong@Passw0rd")
		    .Build();

    private DbConnection _dbConnection = null!;
    private Respawner _respawner = null!;

    public HttpClient HttpClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
	    await _dbContainer.StartAsync();

	    _dbConnection = new SqlConnection(_dbContainer.GetConnectionString());

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
	    builder.UseSetting("ConnectionStrings:SqlServer", _dbContainer.GetConnectionString());
    }

    private async Task InitializeRespawnerAsync()
    {
	    _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
	    {
		    SchemasToInclude = [ "stocks", "carriers", "shipments" ],
		    DbAdapter = DbAdapter.SqlServer
	    });
    }
}

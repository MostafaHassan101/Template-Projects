using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Modules.Carriers.Infrastructure.Database;
using Modules.Carriers.Infrastructure.Policies;
using Modules.Common.Infrastructure.Database;
using Modules.Common.Infrastructure.Policies;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddCarriersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres");

        services.AddDbContext<CarriersDbContext>(x => x
            .UseNpgsql(postgresConnectionString, npgsqlOptions => 
                npgsqlOptions.MigrationsHistoryTable(DbConsts.MigrationHistoryTableName, DbConsts.CarriersSchemaName))
            .UseSnakeCaseNamingConvention()
        );
        
        services.AddScoped<IModuleDatabaseMigrator, CarriersDatabaseMigrator>();
        services.AddSingleton<IPolicyFactory, CarriersPolicyFactory>();

        return services;
    }
}
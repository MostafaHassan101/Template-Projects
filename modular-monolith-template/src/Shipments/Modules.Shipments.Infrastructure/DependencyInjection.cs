using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Modules.Common.Application.Outbox;
using Modules.Common.Application.Saga;
using Modules.Common.Infrastructure.Database;
using Modules.Common.Infrastructure.Policies;
using Modules.Shipments.Infrastructure.Database;
using Modules.Shipments.Infrastructure.Outbox;
using Modules.Shipments.Infrastructure.Policies;
using Modules.Shipments.Infrastructure.Saga;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddShipmentsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var sqlServerConnectionString = configuration.GetConnectionString("SqlServer");

        services.AddDbContext<ShipmentsDbContext>(x => x
            .UseSqlServer(sqlServerConnectionString, sqlServerOptions => 
                sqlServerOptions.MigrationsHistoryTable(DbConsts.MigrationHistoryTableName, DbConsts.ShipmentsSchemaName))
        );
        
        services.AddScoped<IModuleDatabaseMigrator, ShipmentsDatabaseMigrator>();
        services.AddSingleton<IPolicyFactory, ShipmentsPolicyFactory>();

        // Register Outbox and Saga repositories
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<ISagaRepository, SagaRepository>();

        return services;
    }
}
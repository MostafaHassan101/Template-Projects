using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modules.Common.Infrastructure.Database;

namespace Modules.Shipments.Infrastructure.Database;

public class ShipmentsDatabaseMigrator : IModuleDatabaseMigrator
{
    public async Task MigrateAsync(
        IServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ShipmentsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}

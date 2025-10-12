using Microsoft.EntityFrameworkCore;
using Modules.Common.Domain.Outbox;
using Modules.Common.Domain.Saga;
using Modules.Shipments.Domain.Entities;

namespace Modules.Shipments.Infrastructure.Database;

public class ShipmentsDbContext(DbContextOptions<ShipmentsDbContext> options) : DbContext(options)
{
    public DbSet<Shipment> Shipments { get; set; }
    public DbSet<ShipmentItem> ShipmentItems { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<SagaState> SagaStates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(DbConsts.ShipmentsSchemaName);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShipmentsDbContext).Assembly);
    }
}
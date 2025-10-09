using Microsoft.EntityFrameworkCore;
using Modules.Carriers.Domain.Entities;

namespace Modules.Carriers.Infrastructure.Database;

public class CarriersDbContext(DbContextOptions<CarriersDbContext> options) : DbContext(options)
{
    public DbSet<Carrier> Carriers { get; set; }
    public DbSet<CarrierShipment> CarrierShipments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(DbConsts.CarriersSchemaName);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CarriersDbContext).Assembly);
    }
}
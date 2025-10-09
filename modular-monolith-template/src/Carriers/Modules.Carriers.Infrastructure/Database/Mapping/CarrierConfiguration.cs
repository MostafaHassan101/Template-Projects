using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.Carriers.Domain.Entities;

namespace Modules.Carriers.Infrastructure.Database.Mapping;

public class CarrierConfiguration : IEntityTypeConfiguration<Carrier>
{
    public void Configure(EntityTypeBuilder<Carrier> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).IsRequired();
        entity.Property(x => x.IsActive).IsRequired();

        entity.HasMany(x => x.Shipments)
            .WithOne(x => x.Carrier)
            .HasForeignKey(x => x.CarrierId);
    }
}

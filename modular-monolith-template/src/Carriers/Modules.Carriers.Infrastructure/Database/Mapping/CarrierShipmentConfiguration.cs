using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.Carriers.Domain.Entities;

namespace Modules.Carriers.Infrastructure.Database.Mapping;

public class CarrierShipmentConfiguration : IEntityTypeConfiguration<CarrierShipment>
{
    public void Configure(EntityTypeBuilder<CarrierShipment> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.OrderId).IsRequired();
        entity.Property(x => x.CreatedAt).IsRequired();

        entity.OwnsOne(x => x.ShippingAddress, address =>
        {
            address.Property(x => x.Street).IsRequired();
            address.Property(x => x.City).IsRequired();
            address.Property(x => x.Zip).IsRequired();
        });
    }
}

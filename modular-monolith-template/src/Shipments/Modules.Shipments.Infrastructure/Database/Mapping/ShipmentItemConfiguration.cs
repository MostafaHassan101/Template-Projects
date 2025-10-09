using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.Shipments.Domain.Entities;

namespace Modules.Shipments.Infrastructure.Database.Mapping;

public class ShipmentItemConfiguration : IEntityTypeConfiguration<ShipmentItem>
{
    public void Configure(EntityTypeBuilder<ShipmentItem> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id).ValueGeneratedOnAdd();

        entity.Property(x => x.Product).IsRequired();
        entity.Property(x => x.Quantity).IsRequired();
    }
}

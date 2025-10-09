using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.Shipments.Domain.Entities;

namespace Modules.Shipments.Infrastructure.Database.Mapping;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> entity)
    {
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.Number);

        entity.Property(x => x.Number).IsRequired();
        entity.Property(x => x.OrderId).IsRequired();
        entity.Property(x => x.Carrier).IsRequired();
        entity.Property(x => x.ReceiverEmail).IsRequired();

        entity.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired();

        entity.OwnsOne(x => x.Address, ownsBuilder =>
        {
            ownsBuilder.Property(x => x.Street).IsRequired();
            ownsBuilder.Property(x => x.City).IsRequired();
            ownsBuilder.Property(x => x.Zip).IsRequired();
        });

        entity.HasMany(x => x.Items)
            .WithOne(x => x.Shipment)
            .HasForeignKey(x => x.ShipmentId);

        entity.Navigation(x => x.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.Stocks.Domain.Entities;

namespace Modules.Stocks.Infrastructure.Database.Mapping;

public class ProductStockConfiguration : IEntityTypeConfiguration<ProductStock>
{
    public void Configure(EntityTypeBuilder<ProductStock> entity)
    {
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.ProductName).IsUnique();

        entity.Property(x => x.ProductName).IsRequired();
        entity.Property(x => x.AvailableQuantity).IsRequired();
        entity.Property(x => x.LastUpdatedAt).IsRequired();
    }
}

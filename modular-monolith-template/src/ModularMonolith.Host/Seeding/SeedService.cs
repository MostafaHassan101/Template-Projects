using System.Diagnostics.CodeAnalysis;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Modules.Carriers.Domain.Entities;
using Modules.Carriers.Infrastructure.Database;
using Modules.Shipments.Domain.Entities;
using Modules.Shipments.Domain.Enums;
using Modules.Shipments.Domain.ValueObjects;
using Modules.Shipments.Infrastructure.Database;
using Modules.Stocks.Domain.Entities;
using Modules.Stocks.Infrastructure.Database;

namespace ModularMonolith.Host.Seeding;

public class SeedService(
    ShipmentsDbContext shipmentsContext,
    CarriersDbContext carriersContext,
    StocksDbContext stocksContext,
    ILogger<SeedService> logger)
{
    private readonly string[] _carrierNames = ["DHL", "FedEx", "UPS", "USPS"];

    public async Task SeedDataAsync()
    {
	    Randomizer.Seed = new Random(4503);

        if (await shipmentsContext.Shipments.AnyAsync())
        {
            logger.LogInformation("Data already exists, skipping seeding");
            return;
        }

        logger.LogInformation("Starting data seeding...");

        await SeedCarriersAsync();
        var shipmentItems = await SeedShipmentsAsync();
        await SeedStocksAsync(shipmentItems);

        logger.LogInformation("Data seeding completed");
    }

    private async Task SeedCarriersAsync()
    {
        logger.LogInformation("Seeding carriers...");

        var carriers = _carrierNames.Select(name => new Carrier
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = true
        }).ToList();

        await carriersContext.Carriers.AddRangeAsync(carriers);
        await carriersContext.SaveChangesAsync();
    }

    [SuppressMessage("Design", "MA0051:Method is too long")]
    private async Task<List<ShipmentItem>> SeedShipmentsAsync()
    {
        logger.LogInformation("Seeding shipments...");

        var fakeShipments = new Faker<Shipment>()
	        .CustomInstantiator(f => Shipment.Create(
		        f.Commerce.Ean8(),
		        f.Commerce.Ean13(),
		        new Address
		        {
			        Street = f.Address.StreetAddress(),
			        City = f.Address.City(),
			        Zip = f.Address.ZipCode()
		        },
		        f.PickRandom(_carrierNames),
		        f.Internet.Email(),
		        Enumerable.Range(1, f.Random.Int(1, 10))
			        .Select(_ => new ShipmentItem
			        {
				        Product = f.PickRandom(f.Commerce.ProductName()),
				        Quantity = f.Random.Int(1, 5)
			        })
			        .ToList()
	        ));

        var shipments = fakeShipments.Generate(10);

        await shipmentsContext.Shipments.AddRangeAsync(shipments);
        await shipmentsContext.SaveChangesAsync();

        var carrierShipments = shipments
            .Where(s => s.Status != ShipmentStatus.Created)
            .Select(s => new CarrierShipment
            {
                Id = Guid.NewGuid(),
                OrderId = s.OrderId,
                CarrierId = carriersContext.Carriers
                    .First(c => c.Name == s.Carrier).Id,
                CreatedAt = s.CreatedAt,
                ShippingAddress = new Modules.Carriers.Domain.ValueObjects.Address
                {
                    Street = s.Address.Street,
                    City = s.Address.City,
                    Zip = s.Address.Zip
                }
            });

        await carriersContext.CarrierShipments.AddRangeAsync(carrierShipments);
        await carriersContext.SaveChangesAsync();

        return shipments
            .SelectMany(x => x.Items)
            .DistinctBy(x => x.Product)
            .ToList();
    }

    private async Task SeedStocksAsync(List<ShipmentItem> shipmentItems)
    {
        logger.LogInformation("Seeding stocks...");

        var faker = new Faker();
        var stocks = shipmentItems.Select(x =>
        {
            var quantity = faker.Random.Bool(0.15f) ? 0 : faker.Random.Int(1, 100);

            return new ProductStock
            {
                Id = Guid.NewGuid(),
                ProductName = x.Product,
                AvailableQuantity = quantity,
                LastUpdatedAt = faker.Date.Past().ToUniversalTime()
            };
        }).ToList();

        await stocksContext.ProductStocks.AddRangeAsync(stocks);
        await stocksContext.SaveChangesAsync();
    }
}

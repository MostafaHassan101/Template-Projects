using Microsoft.EntityFrameworkCore;
using Modules.Shipments.Domain.Entities;
using Modules.Shipments.Domain.ValueObjects;
using Modules.Shipments.Features.Features.GetShipmentByNumber;
using Modules.Shipments.Infrastructure.Database;

namespace Modules.Shipments.Tests.Unit.Features.GetShipmentByNumber;

public class GetShipmentByNumberHandlerTests : IAsyncDisposable
{
    private readonly ShipmentsDbContext _dbContext;
    private readonly GetShipmentByNumberHandler _handler;

    public GetShipmentByNumberHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ShipmentsDbContext>()
            .UseInMemoryDatabase(databaseName: $"ShipmentsDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new ShipmentsDbContext(options);

        _handler = new GetShipmentByNumberHandler(_dbContext);
    }

    public async ValueTask DisposeAsync()
    {
	    await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task GetShipmentByNumberHandler_ShouldReturnShipment_WhenShipmentExists()
    {
        // Arrange
        var shipment = CreateTestShipment();

        await _dbContext.Shipments.AddAsync(shipment);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(shipment.Number, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(shipment.Number, result.Value.Number);
        Assert.Equal(shipment.OrderId, result.Value.OrderId);
        Assert.Equal(shipment.Status, result.Value.Status);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task GetShipmentByNumberHandler_ShouldReturnNull_WhenShipmentDoesNotExist()
    {
        // Arrange
        const string shipmentNumber = "12345";

        // Act
        var result = await _handler.HandleAsync(shipmentNumber, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.NotNull(result.Errors);

        var error = result.Errors.FirstOrDefault();
        Assert.Equal("Shipments.NotFound", error.Code);
        Assert.Equal("Shipment with number '12345' not found", error.Description);
    }

    private static Shipment CreateTestShipment()
    {
	    return Shipment.Create(
		    "SHP12345",
		    "ORD9876",
		    new Address
		    {
			    Street = "123 Main St",
			    City = "New York",
			    Zip = "10001"
		    },
		    "FedEx",
		    "test@example.com",
		    [
			    new ShipmentItem { Product = "Product1", Quantity = 2 },
			    new ShipmentItem { Product = "Product2", Quantity = 3 }
		    ]);
    }
}

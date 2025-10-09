using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Shipments.Domain.Entities;
using Modules.Shipments.Domain.Enums;
using Modules.Shipments.Domain.ValueObjects;
using Modules.Shipments.Features.Features.ReceiveShipment;
using Modules.Shipments.Infrastructure.Database;
using NSubstitute;

namespace Modules.Shipments.Tests.Unit.Features.ReceiveShipment;

public class ReceiveShipmentHandlerTests : IAsyncDisposable
{
    private readonly ShipmentsDbContext _dbContext;
    private readonly ILogger<ReceiveShipmentHandler> _logger;
    private readonly ReceiveShipmentHandler _handler;

    public ReceiveShipmentHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ShipmentsDbContext>()
            .UseInMemoryDatabase(databaseName: $"ShipmentsDb_{Guid.NewGuid()}")
            .Options;

        var loggerFactory = Substitute.For<ILoggerFactory>();

        _dbContext = new ShipmentsDbContext(options);
        _logger = loggerFactory.CreateLogger<ReceiveShipmentHandler>();

        _handler = new ReceiveShipmentHandler(_dbContext, _logger);
    }

    public async ValueTask DisposeAsync()
    {
	    await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task ReceiveShipmentHandler_ShouldReceiveShipment_WhenShipmentExists()
    {
        // Arrange
        var shipment = CreateTestShipment();
        shipment.Process();
        shipment.Dispatch();
        shipment.Transit();
        shipment.Deliver();

        await _dbContext.Shipments.AddAsync(shipment);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(shipment.Number, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedShipment = await _dbContext.Shipments.FirstOrDefaultAsync(s => s.Number == shipment.Number);
        Assert.NotNull(updatedShipment);
        Assert.Equal(ShipmentStatus.Received, updatedShipment.Status);
    }

    [Fact]
    public async Task ReceiveShipmentHandler_ShouldReturnError_WhenShipmentDoesNotExist()
    {
        // Arrange
        const string shipmentNumber = "NONEXISTENT";

        // Act
        var result = await _handler.HandleAsync(shipmentNumber, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == "Shipment.NotFound");
    }

    [Fact]
    public async Task ReceiveShipmentHandler_ShouldReturnError_WhenShipmentCannotBeReceived()
    {
        // Arrange
        var shipment = CreateTestShipment();

        await _dbContext.Shipments.AddAsync(shipment);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(shipment.Number, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        var updatedShipment = await _dbContext.Shipments.FirstOrDefaultAsync(s => s.Number == shipment.Number);
        Assert.NotNull(updatedShipment);
        Assert.Equal(ShipmentStatus.Created, updatedShipment.Status);
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

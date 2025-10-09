using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Shipments.Domain.Entities;
using Modules.Shipments.Domain.Enums;
using Modules.Shipments.Domain.ValueObjects;
using Modules.Shipments.Features.Features.ProcessShipment;
using Modules.Shipments.Infrastructure.Database;
using NSubstitute;

namespace Modules.Shipments.Tests.Unit.Features.ProcessShipment;

public class ProcessShipmentHandlerTests : IAsyncDisposable
{
    private readonly ShipmentsDbContext _dbContext;
    private readonly ILogger<ProcessShipmentHandler> _logger;
    private readonly ProcessShipmentHandler _handler;

    public ProcessShipmentHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ShipmentsDbContext>()
            .UseInMemoryDatabase(databaseName: $"ShipmentsDb_{Guid.NewGuid()}")
            .Options;

        var loggerFactory = Substitute.For<ILoggerFactory>();

        _dbContext = new ShipmentsDbContext(options);
        _logger = loggerFactory.CreateLogger<ProcessShipmentHandler>();

        _handler = new ProcessShipmentHandler(_dbContext, _logger);
    }

    public async ValueTask DisposeAsync()
    {
	    await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task ProcessShipmentHandler_ShouldProcessShipment_WhenShipmentExists()
    {
        // Arrange
        var shipment = CreateTestShipment();

        await _dbContext.Shipments.AddAsync(shipment);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(shipment.Number, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedShipment = await _dbContext.Shipments.FirstOrDefaultAsync(s => s.Number == shipment.Number);
        Assert.NotNull(updatedShipment);
        Assert.Equal(ShipmentStatus.Processing, updatedShipment.Status);
    }

    [Fact]
    public async Task ProcessShipmentHandler_ShouldReturnError_WhenShipmentDoesNotExist()
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
    public async Task ProcessShipmentHandler_ShouldReturnError_WhenShipmentCannotBeProcessed()
    {
        // Arrange
        var shipment = CreateTestShipment();
        shipment.Process(); // Already processed

        await _dbContext.Shipments.AddAsync(shipment);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(shipment.Number, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        var updatedShipment = await _dbContext.Shipments.FirstOrDefaultAsync(s => s.Number == shipment.Number);
        Assert.NotNull(updatedShipment);
        Assert.Equal(ShipmentStatus.Processing, updatedShipment.Status); // Status should remain unchanged
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

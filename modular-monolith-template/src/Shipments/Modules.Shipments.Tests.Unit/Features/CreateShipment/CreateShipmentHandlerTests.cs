using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Events;
using Modules.Common.Domain.Results;
using Modules.Shipments.Domain.Entities;
using Modules.Shipments.Domain.ValueObjects;
using Modules.Shipments.Features.Features.CreateShipment;
using Modules.Shipments.Features.Features.CreateShipment.Events;
using Modules.Shipments.Features.Features.Shared.Requests;
using Modules.Shipments.Infrastructure.Database;
using Modules.Stocks.PublicApi;
using Modules.Stocks.PublicApi.Contracts;
using NSubstitute;

namespace Modules.Shipments.Tests.Unit.Features.CreateShipment;

public class CreateShipmentHandlerTests : IAsyncDisposable
{
    private readonly ShipmentsDbContext _dbContext;
    private readonly IStockModuleApi _stockApi;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CreateShipmentHandler> _logger;
    private readonly CreateShipmentHandler _handler;

    public CreateShipmentHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ShipmentsDbContext>()
            .UseInMemoryDatabase(databaseName: $"ShipmentsDb_{Guid.NewGuid()}")
            .Options;

        var loggerFactory = Substitute.For<ILoggerFactory>();

        _dbContext = new ShipmentsDbContext(options);
        _stockApi = Substitute.For<IStockModuleApi>();
        _eventPublisher = Substitute.For<IEventPublisher>();
        _logger = loggerFactory.CreateLogger<CreateShipmentHandler>();

        _handler = new CreateShipmentHandler(_dbContext, _stockApi, _eventPublisher, _logger);
    }

    public async ValueTask DisposeAsync()
    {
	    await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task CreateShipmentHandler_ShouldCreateShipment_WhenShipmentDoesNotExist()
    {
        // Arrange
        var request = GetCreateShipmentRequest();

        _stockApi.CheckStockAsync(Arg.Any<CheckStockRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success);

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var shipment = await _dbContext.Shipments.Include(shipment => shipment.Items).FirstOrDefaultAsync(s => s.OrderId == request.OrderId);

        Assert.NotNull(shipment);
        Assert.Equal(2, shipment.Items.Count);

        await _eventPublisher.Received(1).PublishAsync(Arg.Any<ShipmentCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateShipmentHandler_ShouldReturnError_WhenShipmentAlreadyExists()
    {
        // Arrange
        var existingShipment = CreateTestShipment();

        await _dbContext.Shipments.AddAsync(existingShipment);
        await _dbContext.SaveChangesAsync();

        var request = GetCreateShipmentRequest();

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == "Shipments.AlreadyExists");
        Assert.Contains(result.Errors, e => e.Description == $"Shipment for order '{existingShipment.OrderId}' already exists");
    }

    [Fact]
    public async Task CreateShipmentHandler_ShouldReturnError_WhenStockCheckFails()
    {
        // Arrange
        var request = GetCreateShipmentRequest();

        var stockError = Error.Validation("Stock.Insufficient", "Insufficient stock");

        _stockApi.CheckStockAsync(Arg.Any<CheckStockRequest>(), Arg.Any<CancellationToken>())
            .Returns(stockError);

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == "Stock.Insufficient");

        var shipment = await _dbContext.Shipments.FirstOrDefaultAsync(s => s.OrderId == request.OrderId);
        Assert.Null(shipment);
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

    private static CreateShipmentRequest GetCreateShipmentRequest()
    {
	    return new CreateShipmentRequest(
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
			    new ShipmentItemRequest("Product1", 2),
			    new ShipmentItemRequest("Product2", 3)
		    ]);
    }
}

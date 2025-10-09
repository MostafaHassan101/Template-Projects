using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Events;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;
using Modules.Shipments.Features.Features.CreateShipment.Events;
using Modules.Shipments.Features.Features.Shared.Errors;
using Modules.Shipments.Features.Features.Shared.Responses;
using Modules.Shipments.Infrastructure.Database;
using Modules.Stocks.PublicApi;
using Modules.Stocks.PublicApi.Contracts;

namespace Modules.Shipments.Features.Features.CreateShipment;

internal interface ICreateShipmentHandler : IHandler
{
    Task<Result<ShipmentResponse>> HandleAsync(CreateShipmentRequest request, CancellationToken cancellationToken);
}

internal sealed class CreateShipmentHandler(
    ShipmentsDbContext context,
    IStockModuleApi stockApi,
    IEventPublisher eventPublisher,
    ILogger<CreateShipmentHandler> logger)
    : ICreateShipmentHandler
{
    public async Task<Result<ShipmentResponse>> HandleAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var shipmentExists = await context.Shipments.AnyAsync(x => x.OrderId == request.OrderId, cancellationToken);
        if (shipmentExists)
        {
            logger.LogInformation("Shipment for order '{OrderId}' already exists", request.OrderId);
            return ShipmentErrors.AlreadyExists(request.OrderId);
        }

        var stockRequest = CreateCheckStockRequest(request);

        var stockResponse = await stockApi.CheckStockAsync(stockRequest, cancellationToken);
        if (!stockResponse.IsSuccess)
        {
            logger.LogInformation("Stock check failed: {@Errors}", stockResponse.Errors);
            return stockResponse.Errors;
        }

        var shipmentNumber = new Faker().Commerce.Ean8();
        var shipment = request.MapToShipment(shipmentNumber);

        await context.Shipments.AddAsync(shipment, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Created shipment: {@Shipment}", shipment);

        var shipmentCreatedEvent = new ShipmentCreatedEvent(shipment);
        await eventPublisher.PublishAsync(shipmentCreatedEvent, cancellationToken);

        return shipment.MapToResponse();
    }

    private static CheckStockRequest CreateCheckStockRequest(CreateShipmentRequest request)
    {
        return new CheckStockRequest(
            request.Items
                .Select(x => new ProductStock(x.Product, x.Quantity))
                .ToList()
        );
    }
}

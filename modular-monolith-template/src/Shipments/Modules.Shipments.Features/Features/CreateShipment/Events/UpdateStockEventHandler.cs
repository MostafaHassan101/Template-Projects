using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Events;
using Modules.Shipments.Domain.Entities;
using Modules.Stocks.PublicApi;
using Modules.Stocks.PublicApi.Contracts;

namespace Modules.Shipments.Features.Features.CreateShipment.Events;

/// <summary>
/// Event handler that updates stock when a shipment is created
/// </summary>
public sealed class UpdateStockEventHandler(
    IStockModuleApi stockApi,
    ILogger<UpdateStockEventHandler> logger)
    : IEventHandler<ShipmentCreatedEvent>
{
    public async Task HandleAsync(ShipmentCreatedEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating stock for order {OrderId}", @event.Shipment.OrderId);

        try
        {
            var updateRequest = CreateDecreaseStockRequest(@event.Shipment);
            var response = await stockApi.DecreaseStockAsync(updateRequest, cancellationToken);

            if (!response.IsSuccess)
            {
                logger.LogError("Failed to update stock for order {OrderId}: {@Errors}",
                    @event.Shipment.OrderId, response.Errors);

                throw new Exception($"Failed to update stock: {response.Errors}");
            }

            logger.LogInformation("Successfully updated stock for order {OrderId}", @event.Shipment.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update stock for order {OrderId}", @event.Shipment.OrderId);
            throw;
        }
    }

    private static DecreaseStockRequest CreateDecreaseStockRequest(Shipment shipment)
    {
        return new DecreaseStockRequest(
            Products: shipment.Items.Select(x => new ProductStock(x.Product, x.Quantity)).ToList()
        );
    }
}

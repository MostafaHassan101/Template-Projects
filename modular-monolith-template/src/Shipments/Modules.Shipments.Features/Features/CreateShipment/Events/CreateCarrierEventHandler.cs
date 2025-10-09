using Microsoft.Extensions.Logging;
using Modules.Carriers.PublicApi;
using Modules.Carriers.PublicApi.Contracts;
using Modules.Common.Domain.Events;
using Modules.Shipments.Domain.Entities;

namespace Modules.Shipments.Features.Features.CreateShipment.Events;

/// <summary>
/// Event handler that creates a carrier shipment when a shipment is created
/// </summary>
public sealed class CreateCarrierEventHandler(
    ICarrierModuleApi carrierApi,
    ILogger<CreateCarrierEventHandler> logger)
    : IEventHandler<ShipmentCreatedEvent>
{
    public async Task HandleAsync(ShipmentCreatedEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating carrier shipment for order {OrderId}", @event.Shipment.OrderId);

        try
        {
            var carrierRequest = CreateCarrierRequest(@event.Shipment);
            var response = await carrierApi.CreateShipmentAsync(carrierRequest, cancellationToken);

            if (!response.IsSuccess)
            {
                logger.LogError("Failed to create carrier shipment for order {OrderId}: {@Errors}", 
                    @event.Shipment.OrderId, response.Errors);

                throw new Exception($"Failed to create carrier shipment: {response.Errors}");
            }

            logger.LogInformation("Successfully created carrier shipment for order {OrderId}", @event.Shipment.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create carrier shipment for order {OrderId}", @event.Shipment.OrderId);
            throw;
        }
    }

    private static CreateCarrierShipmentRequest CreateCarrierRequest(Shipment shipment)
    {
        return new CreateCarrierShipmentRequest(
            shipment.OrderId,
            new Address(
                shipment.Address.Street,
                shipment.Address.City,
                shipment.Address.Zip
            ),
            shipment.Carrier,
            shipment.ReceiverEmail,
            shipment.Items
                .Select(x => new CarrierShipmentItem(x.Product, x.Quantity))
                .ToList()
        );
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Carriers.Infrastructure.Database;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;

namespace Modules.Carriers.Features.Features.CancelShipment;

internal interface ICancelCarrierShipmentHandler : IHandler
{
    Task<Result<Success>> HandleAsync(string orderId, CancellationToken cancellationToken);
}

internal sealed class CancelCarrierShipmentHandler(
    CarriersDbContext context,
    ILogger<CancelCarrierShipmentHandler> logger)
    : ICancelCarrierShipmentHandler
{
    public async Task<Result<Success>> HandleAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Cancelling carrier shipment for order {OrderId}", orderId);

        var shipment = await context.CarrierShipments
            .FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);

        if (shipment == null)
        {
            logger.LogWarning("Carrier shipment not found for order {OrderId}", orderId);
            // Return success for idempotency - already cancelled or never created
            return Result.Success;
        }

        context.CarrierShipments.Remove(shipment);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully cancelled carrier shipment for order {OrderId}", orderId);

        return Result.Success;
    }
}

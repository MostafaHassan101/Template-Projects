using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;
using Modules.Shipments.Infrastructure.Database;

namespace Modules.Shipments.Features.Features.DeliverShipment;

internal interface IDeliverShipmentHandler : IHandler
{
	Task<Result<Success>> HandleAsync(string shipmentNumber, CancellationToken cancellationToken);
}

internal sealed class DeliverShipmentHandler(
	ShipmentsDbContext context,
	ILogger<DeliverShipmentHandler> logger)
	: IDeliverShipmentHandler
{
	public async Task<Result<Success>> HandleAsync(string shipmentNumber, CancellationToken cancellationToken)
	{
		var shipment = await context.Shipments
			.Where(x => x.Number == shipmentNumber)
			.FirstOrDefaultAsync(cancellationToken: cancellationToken);

		if (shipment is null)
		{
			logger.LogDebug("Shipment with number {ShipmentNumber} not found", shipmentNumber);
			return Error.NotFound("Shipment.NotFound", $"Shipment with number '{shipmentNumber}' not found");
		}

		var response = shipment.Deliver();
		if (response.IsError)
		{
			return response.Errors;
		}

		await context.SaveChangesAsync(cancellationToken);

		logger.LogInformation("Delivered shipment with {ShipmentNumber}", shipmentNumber);
		return Result.Success;
	}
}

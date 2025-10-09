using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;
using Modules.Shipments.Infrastructure.Database;

namespace Modules.Shipments.Features.Features.CancelShipment;

internal interface ICancelShipmentHandler : IHandler
{
	Task<Result<Success>> HandleAsync(string shipmentNumber, CancellationToken cancellationToken);
}

internal sealed class CancelShipmentHandler(
	ShipmentsDbContext context,
	ILogger<CancelShipmentHandler> logger)
	: ICancelShipmentHandler
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

		var response = shipment.Cancel();
		if (response.IsError)
		{
			return response.Errors;
		}

		await context.SaveChangesAsync(cancellationToken);

		logger.LogInformation("Shipment with {ShipmentNumber} was cancelled", shipmentNumber);
		return Result.Success;
	}
}

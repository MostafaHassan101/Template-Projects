using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;
using Modules.Shipments.Infrastructure.Database;

namespace Modules.Shipments.Features.Features.DispatchShipment;

internal interface IDispatchShipmentHandler : IHandler
{
	Task<Result<Success>> HandleAsync(string shipmentNumber, CancellationToken cancellationToken);
}

internal sealed class DispatchShipmentHandler(
	ShipmentsDbContext context,
	ILogger<DispatchShipmentHandler> logger)
	: IDispatchShipmentHandler
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

		var response = shipment.Dispatch();
		if (response.IsError)
		{
			return response.Errors;
		}

		await context.SaveChangesAsync(cancellationToken);

		logger.LogInformation("Dispatched shipment with {ShipmentNumber}", shipmentNumber);
		return Result.Success;
	}
}

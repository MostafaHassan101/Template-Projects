using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;
using Modules.Shipments.Infrastructure.Database;

namespace Modules.Shipments.Features.Features.ProcessShipment;

internal interface IProcessShipmentHandler : IHandler
{
	Task<Result<Success>> HandleAsync(string shipmentNumber, CancellationToken cancellationToken);
}

internal sealed class ProcessShipmentHandler(
	ShipmentsDbContext context,
	ILogger<ProcessShipmentHandler> logger)
	: IProcessShipmentHandler
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

		var response = shipment.Process();
		if (response.IsError)
		{
			return response.Errors;
		}

		await context.SaveChangesAsync(cancellationToken);

		logger.LogInformation("Processed shipment with {ShipmentNumber}", shipmentNumber);
		return Result.Success;
	}
}

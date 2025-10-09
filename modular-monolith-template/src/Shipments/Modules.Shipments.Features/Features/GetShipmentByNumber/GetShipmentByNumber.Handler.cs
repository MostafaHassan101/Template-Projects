using Microsoft.EntityFrameworkCore;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;
using Modules.Shipments.Features.Features.CreateShipment;
using Modules.Shipments.Features.Features.Shared.Errors;
using Modules.Shipments.Features.Features.Shared.Responses;
using Modules.Shipments.Infrastructure.Database;

namespace Modules.Shipments.Features.Features.GetShipmentByNumber;

internal interface IGetShipmentByNumberHandler : IHandler
{
	Task<Result<ShipmentResponse>> HandleAsync(string shipmentNumber, CancellationToken cancellationToken);
}

internal sealed class GetShipmentByNumberHandler(ShipmentsDbContext dbContext)
	: IGetShipmentByNumberHandler
{
	public async Task<Result<ShipmentResponse>> HandleAsync(string shipmentNumber, CancellationToken cancellationToken)
	{
		var shipment = await dbContext.Shipments
			.Include(x => x.Items)
			.FirstOrDefaultAsync(x => x.Number == shipmentNumber, cancellationToken);

		if (shipment is null)
		{
			return ShipmentErrors.NotFound(shipmentNumber);
		}

		var response = shipment.MapToResponse();
		return response;
	}
}

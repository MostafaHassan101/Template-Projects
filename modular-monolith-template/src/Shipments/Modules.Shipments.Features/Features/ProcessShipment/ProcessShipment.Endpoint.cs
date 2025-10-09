using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Modules.Common.API.Abstractions;
using Modules.Common.API.Extensions;
using Modules.Shipments.Features.Features.Shared.Routes;

namespace Modules.Shipments.Features.Features.ProcessShipment;

public class ProcessShipmentEndpoint : IApiEndpoint
{
	public void MapEndpoint(WebApplication app)
	{
		app.MapPost(RouteConsts.ProcessShipment, Handle);
	}

	private static async Task<IResult> Handle(
		[FromRoute] string shipmentNumber,
		IProcessShipmentHandler handler,
		CancellationToken cancellationToken)
	{
		var response = await handler.HandleAsync(shipmentNumber, cancellationToken);
		if (response.IsError)
		{
			return response.Errors.ToProblem();
		}

		return Results.NoContent();
	}
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Modules.Common.API.Abstractions;
using Modules.Common.API.Extensions;
using Modules.Shipments.Features.Features.Shared.Routes;

namespace Modules.Shipments.Features.Features.GetShipmentByNumber;

public class GetShipmentByNumberEndpoint : IApiEndpoint
{
	public void MapEndpoint(WebApplication app)
	{
		app.MapGet(RouteConsts.GetByNumber, Handle);
	}

	private static async Task<IResult> Handle(
		[FromRoute] string shipmentNumber,
		IGetShipmentByNumberHandler handler,
		CancellationToken cancellationToken)
	{
		var response = await handler.HandleAsync(shipmentNumber, cancellationToken);
		if (response.IsError)
		{
			return response.Errors.ToProblem();
		}

		return Results.Ok(response.Value);
	}
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Modules.Carriers.Domain.Policies;
using Modules.Carriers.Features.Features.Shared.Routes;
using Modules.Common.API.Abstractions;
using Modules.Common.API.Extensions;

namespace Modules.Carriers.Features.Features.GetActiveCarriers;

public class GetActiveCarriersApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet(RouteConsts.GetActiveCarriers, Handle)
            .RequireAuthorization(CarrierPolicyConsts.ReadPolicy);
    }

    private static async Task<IResult> Handle(
        [FromServices] IGetActiveCarriersHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(cancellationToken);
        if (response.IsError)
        {
            return response.Errors.ToProblem();
        }

        return Results.Ok(response.Value);
    }
}

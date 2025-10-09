using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Modules.Carriers.Domain.Policies;
using Modules.Carriers.Features.Features.Shared.Routes;
using Modules.Common.API.Abstractions;
using Modules.Common.API.Extensions;

namespace Modules.Carriers.Features.Features.CreateCarrier;

public sealed record CreateCarrierRequest(string Name);

public class CreateCarrierApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost(RouteConsts.Create, Handle)
            .RequireAuthorization(CarrierPolicyConsts.CreatePolicy);
    }

    private static async Task<IResult> Handle(
        [FromBody] CreateCarrierRequest request,
        IValidator<CreateCarrierRequest> validator,
        ICreateCarrierHandler handler,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var response = await handler.HandleAsync(request, cancellationToken);
        if (response.IsError)
        {
            return response.Errors.ToProblem();
        }

        return Results.Ok(response.Value);
    }
}

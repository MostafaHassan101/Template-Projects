using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Modules.Common.API.Abstractions;
using Modules.Common.API.Extensions;
using Modules.Stocks.Domain.Policies;
using Modules.Stocks.Features.Features.Shared.Routes;

namespace Modules.Stocks.Features.Features.CreateStock;

public sealed record CreateStockRequest(string ProductName, int Quantity);

public sealed record CreateStockResponse(string ProductName, int Quantity);

public class CreateStockApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost(RouteConsts.Create, Handle)
            .RequireAuthorization(StockPolicyConsts.CreatePolicy);
    }

    private static async Task<IResult> Handle(
        [FromBody] CreateStockRequest request,
        IValidator<CreateStockRequest> validator,
        ICreateStockHandler handler,
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

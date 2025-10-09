using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Modules.Common.API.Abstractions;
using Modules.Common.API.Extensions;
using Modules.Stocks.Domain.Policies;
using Modules.Stocks.Features.Features.Shared.Routes;

namespace Modules.Stocks.Features.Features.IncreaseStock;

public sealed record IncreaseStockRequest(string ProductName, int Quantity);

public class IncreaseStockApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost(RouteConsts.IncreaseStock, Handle)
            .RequireAuthorization(StockPolicyConsts.UpdatePolicy);
    }

    private static async Task<IResult> Handle(
        [FromBody] IncreaseStockRequest request,
        IValidator<IncreaseStockRequest> validator,
        IIncreaseStockHandler handler,
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

        return Results.NoContent();
    }
}

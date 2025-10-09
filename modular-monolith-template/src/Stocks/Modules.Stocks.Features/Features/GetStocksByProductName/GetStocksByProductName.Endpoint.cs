using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Modules.Common.API.Abstractions;
using Modules.Common.API.Extensions;
using Modules.Stocks.Domain.Policies;
using Modules.Stocks.Features.Features.Shared.Routes;

namespace Modules.Stocks.Features.Features.GetStocksByProductName;

public sealed record GetStocksByProductNameRequest(string ProductName);

public sealed record StockResponse(string ProductName, int Quantity);

public class GetStocksByProductNameApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet(RouteConsts.GetStocksByProductName, Handle)
            .RequireAuthorization(StockPolicyConsts.ReadPolicy);
    }

    private static async Task<IResult> Handle(
        [FromQuery] string productName,
        IValidator<GetStocksByProductNameRequest> validator,
        IGetStocksByProductNameHandler handler,
        CancellationToken cancellationToken)
    {
        var request = new GetStocksByProductNameRequest(productName);
        
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

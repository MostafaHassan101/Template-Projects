using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;
using Modules.Stocks.Features.Features.Shared.Errors;
using Modules.Stocks.Infrastructure.Database;

namespace Modules.Stocks.Features.Features.GetStocksByProductName;

internal interface IGetStocksByProductNameHandler : IHandler
{
    Task<Result<StockResponse>> HandleAsync(GetStocksByProductNameRequest request, CancellationToken cancellationToken);
}

internal sealed class GetStocksByProductNameHandler(
    StocksDbContext context,
    ILogger<GetStocksByProductNameHandler> logger)
    : IGetStocksByProductNameHandler
{
    public async Task<Result<StockResponse>> HandleAsync(
        GetStocksByProductNameRequest request,
        CancellationToken cancellationToken)
    {
        var stock = await context.ProductStocks
            .FirstOrDefaultAsync(x => x.ProductName == request.ProductName, cancellationToken);

        if (stock is null)
        {
            logger.LogInformation("Product '{ProductName}' not found in stock", request.ProductName);
            return StockErrors.ProductNotFound(request.ProductName);
        }

        logger.LogInformation("Retrieved stock for product '{ProductName}': {Quantity}", 
            request.ProductName, stock.AvailableQuantity);

        return MapToStockResponse(stock);
    }

    private static StockResponse MapToStockResponse(Domain.Entities.ProductStock stock)
        => new(stock.ProductName, stock.AvailableQuantity);
}

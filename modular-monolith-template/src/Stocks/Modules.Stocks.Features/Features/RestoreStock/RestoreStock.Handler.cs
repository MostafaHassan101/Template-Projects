using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;
using Modules.Stocks.Infrastructure.Database;
using Modules.Stocks.PublicApi.Contracts;

namespace Modules.Stocks.Features.Features.RestoreStock;

internal interface IRestoreStockHandler : IHandler
{
    Task<Result<Success>> HandleAsync(DecreaseStockRequest request, CancellationToken cancellationToken);
}

internal sealed class RestoreStockHandler(
    StocksDbContext context,
    ILogger<RestoreStockHandler> logger)
    : IRestoreStockHandler
{
    public async Task<Result<Success>> HandleAsync(
        DecreaseStockRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Restoring stock for {ProductCount} products", request.Products.Count);

        foreach (var productStock in request.Products)
        {
            var stock = await context.ProductStocks
                .FirstOrDefaultAsync(x => x.ProductName == productStock.ProductName, cancellationToken);

            if (stock == null)
            {
                logger.LogWarning("Stock not found for product {ProductName} during restoration", productStock.ProductName);
                // Continue with other products
                continue;
            }

            // Restore the quantity (add back what was decreased)
            stock.AvailableQuantity += productStock.Quantity;
            logger.LogInformation("Restored {Quantity} units for product {ProductName}. New quantity: {NewQuantity}",
                productStock.Quantity, productStock.ProductName, stock.AvailableQuantity);
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully restored stock for {ProductCount} products", request.Products.Count);

        return Result.Success;
    }
}

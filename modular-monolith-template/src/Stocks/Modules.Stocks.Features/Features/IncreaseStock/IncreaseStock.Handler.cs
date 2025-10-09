using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;
using Modules.Stocks.Features.Features.Shared.Errors;
using Modules.Stocks.Infrastructure.Database;

namespace Modules.Stocks.Features.Features.IncreaseStock;

internal interface IIncreaseStockHandler : IHandler
{
    Task<Result<Success>> HandleAsync(IncreaseStockRequest request, CancellationToken cancellationToken);
}

internal sealed class IncreaseStockHandler(
    StocksDbContext context,
    ILogger<IncreaseStockHandler> logger)
    : IIncreaseStockHandler
{
    public async Task<Result<Success>> HandleAsync(
        IncreaseStockRequest request,
        CancellationToken cancellationToken)
    {
        var existingStock = await context.ProductStocks
            .FirstOrDefaultAsync(x => x.ProductName == request.ProductName, cancellationToken);

        if (existingStock is null)
        {
            return StockErrors.ProductNotFound(request.ProductName);
        }

        existingStock.AvailableQuantity += request.Quantity;
        existingStock.LastUpdatedAt = DateTime.UtcNow;
            
        logger.LogInformation("Increased stock for product '{ProductName}' by {Quantity}. New quantity: {NewQuantity}", 
            request.ProductName, request.Quantity, existingStock.AvailableQuantity);

        await context.SaveChangesAsync(cancellationToken);
        
        return Result.Success;
    }
}

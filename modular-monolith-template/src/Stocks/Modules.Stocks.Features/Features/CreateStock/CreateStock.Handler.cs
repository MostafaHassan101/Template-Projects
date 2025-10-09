using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;
using Modules.Stocks.Domain.Entities;
using Modules.Stocks.Features.Features.Shared.Errors;
using Modules.Stocks.Infrastructure.Database;

namespace Modules.Stocks.Features.Features.CreateStock;

internal interface ICreateStockHandler : IHandler
{
    Task<Result<CreateStockResponse>> HandleAsync(CreateStockRequest request, CancellationToken cancellationToken);
}

internal sealed class CreateStockHandler(
    StocksDbContext context,
    ILogger<CreateStockHandler> logger)
    : ICreateStockHandler
{
    public async Task<Result<CreateStockResponse>> HandleAsync(
        CreateStockRequest request,
        CancellationToken cancellationToken)
    {
        var existingStock = await context.ProductStocks
            .FirstOrDefaultAsync(x => x.ProductName == request.ProductName, cancellationToken);

        if (existingStock is not null)
        {
            logger.LogInformation("Stock for product '{ProductName}' already exists", request.ProductName);
            return StockErrors.ProductAlreadyExists(request.ProductName);
        }

        var productStock = CreateProductStock(request);
        
        await context.ProductStocks.AddAsync(productStock, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Created stock for product '{ProductName}' with quantity {Quantity}", 
            request.ProductName, request.Quantity);

        return MapToCreateStockResponse(productStock);
    }

    private static ProductStock CreateProductStock(CreateStockRequest request)
        => new()
        {
            Id = Guid.NewGuid(),
            ProductName = request.ProductName,
            AvailableQuantity = request.Quantity,
            LastUpdatedAt = DateTime.UtcNow
        };

    private static CreateStockResponse MapToCreateStockResponse(ProductStock stock)
        => new(stock.ProductName, stock.AvailableQuantity);
}

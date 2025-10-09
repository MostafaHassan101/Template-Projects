using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;
using Modules.Stocks.Features.Features.Shared.Errors;
using Modules.Stocks.Infrastructure.Database;
using Modules.Stocks.PublicApi.Contracts;

namespace Modules.Stocks.Features.Features.DecreaseStock;

internal interface IDecreaseStockHandler : IHandler
{
    Task<Result<Success>> HandleAsync(DecreaseStockRequest request, CancellationToken cancellationToken);
}

internal sealed class DecreaseStockHandler(
    StocksDbContext dbContext,
    IValidator<DecreaseStockRequest> validator)
    : IDecreaseStockHandler
{
    public async Task<Result<Success>> HandleAsync(
        DecreaseStockRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToDomainErrors();
        }

        var stocks = await GetProductStocksAsync(request.Products, cancellationToken);

        var errors = VerifyProductQuantities(request.Products, stocks).ToList();
        if (errors.Count > 0)
        {
            return errors;
        }

        foreach (var product in request.Products)
        {
            var stock = stocks[product.ProductName];

            stock.AvailableQuantity -= product.Quantity;
            stock.LastUpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }

    private static IEnumerable<Error> VerifyProductQuantities(
        List<ProductStock> products,
        Dictionary<string, Domain.Entities.ProductStock> stocks)
    {
        foreach (var product in products)
        {
            if (!stocks.TryGetValue(product.ProductName, out var stock))
            {
                yield return StockErrors.ProductNotFound(product.ProductName);
                continue;
            }

            if (stock.AvailableQuantity < product.Quantity)
            {
                yield return StockErrors.InsufficientStocks(product.ProductName, product.Quantity, stock.AvailableQuantity);
            }
        }
    }

    private async Task<Dictionary<string, Domain.Entities.ProductStock>> GetProductStocksAsync(
        List<ProductStock> products,
        CancellationToken cancellationToken)
    {
        var productNames = products.Select(x => x.ProductName).ToList();

        var stocks = await dbContext.ProductStocks
            .Where(x => productNames.Contains(x.ProductName))
            .ToDictionaryAsync(x => x.ProductName, x => x, cancellationToken);

        return stocks;
    }
}

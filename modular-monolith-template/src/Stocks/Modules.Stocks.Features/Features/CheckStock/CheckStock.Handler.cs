using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;
using Modules.Stocks.Features.Features.Shared.Errors;
using Modules.Stocks.Infrastructure.Database;
using Modules.Stocks.PublicApi.Contracts;

namespace Modules.Stocks.Features.Features.CheckStock;

internal interface ICheckStockHandler : IHandler
{
    Task<Result<Success>> HandleAsync(CheckStockRequest request, CancellationToken cancellationToken);
}

internal sealed class CheckStockHandler(
    StocksDbContext dbContext,
    IValidator<CheckStockRequest> validator)
    : ICheckStockHandler
{
    public async Task<Result<Success>> HandleAsync(
        CheckStockRequest request,
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

        return Result.Success;
    }

    private static IEnumerable<Error> VerifyProductQuantities(List<ProductStock> products, Dictionary<string, int> stocks)
    {
        foreach (var product in products)
        {
            if (!stocks.TryGetValue(product.ProductName, out var availableQuantity))
            {
                yield return StockErrors.ProductNotFound(product.ProductName);
                continue;
            }

            if (availableQuantity < product.Quantity)
            {
                yield return StockErrors.InsufficientStocks(product.ProductName, product.Quantity, availableQuantity);
            }
        }
    }

    private async Task<Dictionary<string, int>> GetProductStocksAsync(List<ProductStock> products, CancellationToken cancellationToken)
    {
        var productNames = products.Select(x => x.ProductName).ToList();

        var stocks = await dbContext.ProductStocks
            .Where(x => productNames.Contains(x.ProductName))
            .ToDictionaryAsync(x => x.ProductName, x => x.AvailableQuantity, cancellationToken);

        return stocks;
    }
}

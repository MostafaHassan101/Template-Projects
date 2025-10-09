using System.Diagnostics;
using Modules.Common.Domain.Results;
using Modules.Stocks.Features.Tracing;
using Modules.Stocks.PublicApi;
using Modules.Stocks.PublicApi.Contracts;

namespace Modules.Stocks.Features.InternalApi.Decorators;

public class TracedStockModuleApi(IStockModuleApi inner) : IStockModuleApi
{
    public async Task<Result<Success>> CheckStockAsync(
        CheckStockRequest request,
        CancellationToken cancellationToken)
    {
        using var activity = StocksActivitySource.Instance.StartActivity($"{StocksActivitySource.Instance.Name}.check-stock");

        activity?.SetTag("module", StocksActivitySource.Instance.Name);
        activity?.SetTag("operation", "CheckStock");
        activity?.SetTag("product.count", request.Products?.Count ?? 0);

        try
        {
            var response = await inner.CheckStockAsync(request, cancellationToken);

            activity?.SetTag("stock.available", response.IsSuccess);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            throw;
        }
    }

    public async Task<Result<Success>> DecreaseStockAsync(
        DecreaseStockRequest request,
        CancellationToken cancellationToken)
    {
        using var activity = StocksActivitySource.Instance.StartActivity($"{StocksActivitySource.Instance.Name}.update-stock");

        activity?.SetTag("module", StocksActivitySource.Instance.Name);
        activity?.SetTag("operation", "UpdateStock");
        activity?.SetTag("items.count", request.Products.Count);

        try
        {
            var response = await inner.DecreaseStockAsync(request, cancellationToken);

            activity?.SetTag("update.successful", response.IsSuccess);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            throw;
        }
    }
}

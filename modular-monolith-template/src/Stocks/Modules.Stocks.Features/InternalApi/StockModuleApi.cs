using Modules.Common.Domain.Results;
using Modules.Stocks.Features.Features.CheckStock;
using Modules.Stocks.Features.Features.DecreaseStock;
using Modules.Stocks.PublicApi;
using Modules.Stocks.PublicApi.Contracts;

namespace Modules.Stocks.Features.InternalApi;

internal sealed class StockModuleApi(
    ICheckStockHandler checkStockHandler,
    IDecreaseStockHandler decreaseStockHandler) : IStockModuleApi
{
    public async Task<Result<Success>> CheckStockAsync(
        CheckStockRequest request,
        CancellationToken cancellationToken)
    {
        return await checkStockHandler.HandleAsync(request, cancellationToken);
    }

    public async Task<Result<Success>> DecreaseStockAsync(
        DecreaseStockRequest request,
        CancellationToken cancellationToken)
    {
        return await decreaseStockHandler.HandleAsync(request, cancellationToken);
    }
}

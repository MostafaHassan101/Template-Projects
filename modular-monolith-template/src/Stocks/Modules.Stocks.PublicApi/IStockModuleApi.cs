using Modules.Common.Domain.Results;
using Modules.Stocks.PublicApi.Contracts;

namespace Modules.Stocks.PublicApi;

public interface IStockModuleApi
{
    Task<Result<Success>> CheckStockAsync(
        CheckStockRequest request,
        CancellationToken cancellationToken);

    Task<Result<Success>> DecreaseStockAsync(
        DecreaseStockRequest request,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Compensating transaction: Restores stock quantities
    /// </summary>
    Task<Result<Success>> RestoreStockAsync(
        DecreaseStockRequest request,
        CancellationToken cancellationToken);
}

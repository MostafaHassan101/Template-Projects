using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Results;
using Modules.Common.Domain.Saga;
using Modules.Stocks.PublicApi;
using Modules.Stocks.PublicApi.Contracts;

namespace Modules.Shipments.Features.Saga.Steps;

/// <summary>
/// Step 3: Decrease stock quantities
/// </summary>
public class DecreaseStockStep : ISagaStep<CreateShipmentSagaData>
{
    public string StepName => "DecreaseStock";

    public Task<Result<Success>> ExecuteAsync(CreateShipmentSagaData data, CancellationToken cancellationToken)
    {
        // Actual implementation in handler
        return Task.FromResult<Result<Success>>(Result.Success);
    }

    public Task<Result<Success>> CompensateAsync(CreateShipmentSagaData data, CancellationToken cancellationToken)
    {
        // Compensation handled in handler
        return Task.FromResult<Result<Success>>(Result.Success);
    }
}

/// <summary>
/// Injectable version of DecreaseStockStep
/// </summary>
internal sealed class DecreaseStockStepHandler(
    IStockModuleApi stockApi,
    ILogger<DecreaseStockStepHandler> logger)
{
    public async Task<Result<Success>> ExecuteAsync(CreateShipmentSagaData data, CancellationToken cancellationToken)
    {
        logger.LogInformation("Decreasing stock for order {OrderId}", data.Request.OrderId);

        var decreaseRequest = new DecreaseStockRequest(
            Products: data.Request.Items
                .Select(x => new ProductStock(x.Product, x.Quantity))
                .ToList()
        );

        var result = await stockApi.DecreaseStockAsync(decreaseRequest, cancellationToken);

        if (result.IsError)
        {
            logger.LogError("Failed to decrease stock for order {OrderId}: {@Errors}", 
                data.Request.OrderId, result.Errors);
            return result.Errors;
        }

        logger.LogInformation("Stock decreased successfully for order {OrderId}", data.Request.OrderId);
        return Result.Success;
    }

    public async Task<Result<Success>> CompensateAsync(CreateShipmentSagaData data, CancellationToken cancellationToken)
    {
        logger.LogWarning("Compensating: Restoring stock for order {OrderId}", data.Request.OrderId);

        var restoreRequest = new DecreaseStockRequest(
            Products: data.Request.Items
                .Select(x => new ProductStock(x.Product, x.Quantity))
                .ToList()
        );

        var result = await stockApi.RestoreStockAsync(restoreRequest, cancellationToken);

        if (result.IsError)
        {
            logger.LogError("Failed to restore stock during compensation for order {OrderId}: {@Errors}", 
                data.Request.OrderId, result.Errors);
            return result.Errors;
        }

        logger.LogInformation("Stock restored successfully for order {OrderId}", data.Request.OrderId);
        return Result.Success;
    }
}

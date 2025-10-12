using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Results;
using Modules.Common.Domain.Saga;
using Modules.Stocks.PublicApi;
using Modules.Stocks.PublicApi.Contracts;

namespace Modules.Shipments.Features.Saga.Steps;

/// <summary>
/// Step 1: Validate stock availability
/// </summary>
public class ValidateStockStep : ISagaStep<CreateShipmentSagaData>
{
    public string StepName => "ValidateStock";

    public Task<Result<Success>> ExecuteAsync(CreateShipmentSagaData data, CancellationToken cancellationToken)
    {
        // This step is injected via service provider in the handler
        // For now, return success - actual implementation in handler
        return Task.FromResult<Result<Success>>(Result.Success);
    }

    public Task<Result<Success>> CompensateAsync(CreateShipmentSagaData data, CancellationToken cancellationToken)
    {
        // No compensation needed for validation
        return Task.FromResult<Result<Success>>(Result.Success);
    }
}

/// <summary>
/// Injectable version of ValidateStockStep
/// </summary>
internal sealed class ValidateStockStepHandler(
    IStockModuleApi stockApi,
    ILogger<ValidateStockStepHandler> logger)
{
    public async Task<Result<Success>> ExecuteAsync(CreateShipmentSagaData data, CancellationToken cancellationToken)
    {
        logger.LogInformation("Validating stock for order {OrderId}", data.Request.OrderId);

        var stockRequest = new CheckStockRequest(
            data.Request.Items
                .Select(x => new ProductStock(x.Product, x.Quantity))
                .ToList()
        );

        var result = await stockApi.CheckStockAsync(stockRequest, cancellationToken);

        if (result.IsError)
        {
            logger.LogWarning("Stock validation failed for order {OrderId}: {@Errors}", 
                data.Request.OrderId, result.Errors);
            return result.Errors;
        }

        logger.LogInformation("Stock validation successful for order {OrderId}", data.Request.OrderId);
        return Result.Success;
    }
}

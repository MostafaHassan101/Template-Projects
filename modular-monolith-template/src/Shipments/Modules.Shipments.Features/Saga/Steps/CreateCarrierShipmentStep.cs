using Microsoft.Extensions.Logging;
using Modules.Carriers.PublicApi;
using Modules.Carriers.PublicApi.Contracts;
using Modules.Common.Domain.Results;
using Modules.Common.Domain.Saga;

namespace Modules.Shipments.Features.Saga.Steps;

/// <summary>
/// Step 4: Create carrier shipment
/// </summary>
public class CreateCarrierShipmentStep : ISagaStep<CreateShipmentSagaData>
{
    public string StepName => "CreateCarrierShipment";

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
/// Injectable version of CreateCarrierShipmentStep
/// </summary>
internal sealed class CreateCarrierShipmentStepHandler(
    ICarrierModuleApi carrierApi,
    ILogger<CreateCarrierShipmentStepHandler> logger)
{
    public async Task<Result<Success>> ExecuteAsync(CreateShipmentSagaData data, CancellationToken cancellationToken)
    {
        if (data.CreatedShipment == null)
        {
            return Error.Failure("CreateCarrierShipment.NoShipment", "Shipment not created");
        }

        logger.LogInformation("Creating carrier shipment for order {OrderId}", data.Request.OrderId);

        var carrierRequest = new CreateCarrierShipmentRequest(
            data.Request.OrderId,
            new Address(
                data.Request.Address.Street,
                data.Request.Address.City,
                data.Request.Address.Zip
            ),
            data.Request.Carrier,
            data.Request.ReceiverEmail,
            data.CreatedShipment.Items
                .Select(x => new CarrierShipmentItem(x.Product, x.Quantity))
                .ToList()
        );

        var result = await carrierApi.CreateShipmentAsync(carrierRequest, cancellationToken);

        if (result.IsError)
        {
            logger.LogError("Failed to create carrier shipment for order {OrderId}: {@Errors}", 
                data.Request.OrderId, result.Errors);
            return result.Errors;
        }

        logger.LogInformation("Carrier shipment created successfully for order {OrderId}", data.Request.OrderId);
        return Result.Success;
    }

    public async Task<Result<Success>> CompensateAsync(CreateShipmentSagaData data, CancellationToken cancellationToken)
    {
        logger.LogWarning("Compensating: Cancelling carrier shipment for order {OrderId}", data.Request.OrderId);

        var result = await carrierApi.CancelShipmentAsync(data.Request.OrderId, cancellationToken);

        if (result.IsError)
        {
            logger.LogError("Failed to cancel carrier shipment during compensation for order {OrderId}: {@Errors}", 
                data.Request.OrderId, result.Errors);
            return result.Errors;
        }

        logger.LogInformation("Carrier shipment cancelled successfully for order {OrderId}", data.Request.OrderId);
        return Result.Success;
    }
}

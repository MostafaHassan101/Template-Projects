using Bogus;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Results;
using Modules.Common.Domain.Saga;
using Modules.Shipments.Features.Features.CreateShipment;
using Modules.Shipments.Infrastructure.Database;

namespace Modules.Shipments.Features.Saga.Steps;

/// <summary>
/// Step 2: Create the shipment entity
/// </summary>
public class CreateShipmentStep : ISagaStep<CreateShipmentSagaData>
{
    public string StepName => "CreateShipment";

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
/// Injectable version of CreateShipmentStep
/// </summary>
internal sealed class CreateShipmentStepHandler(
    ShipmentsDbContext context,
    ILogger<CreateShipmentStepHandler> logger)
{
    public async Task<Result<Success>> ExecuteAsync(CreateShipmentSagaData data, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating shipment for order {OrderId}", data.Request.OrderId);

        var shipmentNumber = new Faker().Commerce.Ean8();
        var shipment = data.Request.MapToShipment(shipmentNumber);

        await context.Shipments.AddAsync(shipment, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // Store for later steps
        data.CreatedShipment = shipment;
        data.ShipmentNumber = shipmentNumber;

        logger.LogInformation("Shipment created with number {ShipmentNumber} for order {OrderId}", 
            shipmentNumber, data.Request.OrderId);

        return Result.Success;
    }

    public async Task<Result<Success>> CompensateAsync(CreateShipmentSagaData data, CancellationToken cancellationToken)
    {
        if (data.CreatedShipment == null)
        {
            return Result.Success; // Nothing to compensate
        }

        logger.LogWarning("Compensating: Deleting shipment {ShipmentNumber}", data.ShipmentNumber);

        context.Shipments.Remove(data.CreatedShipment);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Shipment {ShipmentNumber} deleted successfully", data.ShipmentNumber);

        return Result.Success;
    }
}

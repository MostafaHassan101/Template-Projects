using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Common.Application.Saga;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;
using Modules.Common.Domain.Saga;
using Modules.Shipments.Features.Features.Shared.Errors;
using Modules.Shipments.Features.Features.Shared.Responses;
using Modules.Shipments.Features.Saga;
using Modules.Shipments.Infrastructure.Database;

namespace Modules.Shipments.Features.Features.CreateShipment;

internal interface ICreateShipmentHandler : IHandler
{
    Task<Result<ShipmentResponse>> HandleAsync(CreateShipmentRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Handler for creating shipments using Saga + Outbox pattern for reliable distributed transactions
/// </summary>
internal sealed class CreateShipmentHandler(
    ShipmentsDbContext context,
    CreateShipmentSagaOrchestrator sagaOrchestrator,
    ISagaRepository sagaRepository,
    ILogger<CreateShipmentHandler> logger)
    : ICreateShipmentHandler
{
    public async Task<Result<ShipmentResponse>> HandleAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var idempotencyCheck = await CheckIdempotencyAsync(request, cancellationToken);
        if (idempotencyCheck.HasValue)
        {
            return idempotencyCheck.Value;
        }

        var sagaData = new CreateShipmentSagaData { Request = request };
        var sagaResult = await sagaOrchestrator.ExecuteAsync(sagaData, request.OrderId, cancellationToken);

        if (sagaResult.IsError)
        {
            logger.LogError("Saga execution failed for order '{OrderId}': {@Errors}", 
                request.OrderId, sagaResult.Errors);
            return sagaResult.Errors;
        }

        return ValidateAndReturnShipment(sagaData, request.OrderId);
    }

    private async Task<Result<ShipmentResponse>?> CheckIdempotencyAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var shipmentExists = await context.Shipments.AnyAsync(x => x.OrderId == request.OrderId, cancellationToken);
        if (shipmentExists)
        {
            logger.LogInformation("Shipment for order '{OrderId}' already exists", request.OrderId);
            return ShipmentErrors.AlreadyExists(request.OrderId);
        }

        var existingSaga = await sagaRepository.GetByCorrelationIdAsync(request.OrderId, cancellationToken);
        if (existingSaga != null)
        {
            return await HandleExistingSagaAsync(existingSaga, request.OrderId, cancellationToken);
        }

        return null;
    }

    private async Task<Result<ShipmentResponse>?> HandleExistingSagaAsync(
        SagaState existingSaga,
        string orderId,
        CancellationToken cancellationToken)
    {
        if (existingSaga.IsCompleted)
        {
            logger.LogInformation("Saga already completed for order '{OrderId}'", orderId);
            var existingShipment = await context.Shipments
                .FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
            
            if (existingShipment != null)
            {
                return existingShipment.MapToResponse();
            }
        }
        else if (existingSaga.IsFailed)
        {
            logger.LogWarning("Previous saga failed for order '{OrderId}'. Creating new saga.", orderId);
            return null; // Allow retry
        }
        else
        {
            logger.LogWarning("Saga in progress for order '{OrderId}'", orderId);
            return Error.Conflict("Shipment.SagaInProgress", 
                $"Shipment creation is already in progress for order {orderId}");
        }

        return null;
    }

    private Result<ShipmentResponse> ValidateAndReturnShipment(CreateShipmentSagaData sagaData, string orderId)
    {
        if (sagaData.CreatedShipment == null)
        {
            logger.LogError("Saga completed but shipment is null for order '{OrderId}'", orderId);
            return Error.Unexpected("Shipment.CreationFailed", "Shipment creation failed unexpectedly");
        }

        logger.LogInformation("Shipment created successfully via saga for order '{OrderId}'", orderId);
        return sagaData.CreatedShipment.MapToResponse();
    }
}

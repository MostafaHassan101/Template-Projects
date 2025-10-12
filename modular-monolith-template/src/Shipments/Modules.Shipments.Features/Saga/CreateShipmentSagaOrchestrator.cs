using Microsoft.Extensions.Logging;
using Modules.Common.Application.Saga;
using Modules.Common.Domain.Results;
using Modules.Common.Domain.Saga;
using Modules.Shipments.Features.Saga.Steps;

namespace Modules.Shipments.Features.Saga;

/// <summary>
/// Specialized orchestrator for CreateShipment saga with dependency injection support
/// </summary>
internal sealed class CreateShipmentSagaOrchestrator(
    ISagaRepository sagaRepository,
    ValidateStockStepHandler validateStockHandler,
    CreateShipmentStepHandler createShipmentHandler,
    DecreaseStockStepHandler decreaseStockHandler,
    CreateCarrierShipmentStepHandler createCarrierShipmentHandler,
    ILogger<CreateShipmentSagaOrchestrator> logger)
{
    public async Task<Result<Success>> ExecuteAsync(
        CreateShipmentSagaData data,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var sagaState = await InitializeSagaAsync(data, correlationId, cancellationToken);
        var executedSteps = new List<string>();

        try
        {
            sagaState.Status = SagaStatus.InProgress;
            await sagaRepository.UpdateAsync(sagaState, cancellationToken);

            var result = await ExecuteStepsAsync(sagaState, data, executedSteps, cancellationToken);
            if (result.IsError)
            {
                return result.Errors;
            }

            await CompleteSagaAsync(sagaState, data, cancellationToken);
            return Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in CreateShipment saga {SagaId}", sagaState.Id);
            await CompensateAndFailAsync(sagaState, data, executedSteps, 
                [Error.Unexpected("Saga.UnexpectedError", ex.Message)], cancellationToken);
            return Error.Unexpected("Saga.ExecutionFailed", $"Saga execution failed: {ex.Message}");
        }
    }

    private async Task<SagaState> InitializeSagaAsync(
        CreateShipmentSagaData data,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var sagaState = new SagaState
        {
            Id = Guid.NewGuid(),
            SagaType = "CreateShipment",
            CorrelationId = correlationId,
            Status = SagaStatus.Started,
            CurrentStep = "Initial",
            Payload = System.Text.Json.JsonSerializer.Serialize(data.Request),
            CreatedAt = DateTime.UtcNow
        };

        await sagaRepository.AddAsync(sagaState, cancellationToken);
        logger.LogInformation("Started CreateShipment saga with ID {SagaId} for order {OrderId}", 
            sagaState.Id, data.Request.OrderId);

        return sagaState;
    }

    private async Task<Result<Success>> ExecuteStepsAsync(
        SagaState sagaState,
        CreateShipmentSagaData data,
        List<string> executedSteps,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteStepAsync(sagaState, "ValidateStock", data, executedSteps, 
            () => validateStockHandler.ExecuteAsync(data, cancellationToken), cancellationToken);
        if (result.IsError)
        {
            return result;
        }

        result = await ExecuteStepAsync(sagaState, "CreateShipment", data, executedSteps, 
            () => createShipmentHandler.ExecuteAsync(data, cancellationToken), cancellationToken);
        if (result.IsError)
        {
            return result;
        }

        result = await ExecuteStepAsync(sagaState, "DecreaseStock", data, executedSteps, 
            () => decreaseStockHandler.ExecuteAsync(data, cancellationToken), cancellationToken);
        if (result.IsError)
        {
            return result;
        }

        result = await ExecuteStepAsync(sagaState, "CreateCarrierShipment", data, executedSteps, 
            () => createCarrierShipmentHandler.ExecuteAsync(data, cancellationToken), cancellationToken);
        
        return result;
    }

    private async Task<Result<Success>> ExecuteStepAsync(
        SagaState sagaState,
        string stepName,
        CreateShipmentSagaData data,
        List<string> executedSteps,
        Func<Task<Result<Success>>> stepAction,
        CancellationToken cancellationToken)
    {
        sagaState.CurrentStep = stepName;
        await sagaRepository.UpdateAsync(sagaState, cancellationToken);
        logger.LogInformation("Executing step: {StepName}", stepName);

        var result = await stepAction();
        if (result.IsError)
        {
            logger.LogError("{StepName} step failed: {@Errors}", stepName, result.Errors);
            if (executedSteps.Count == 0)
            {
                await FailSagaAsync(sagaState, result.Errors, cancellationToken);
            }
            else
            {
                await CompensateAndFailAsync(sagaState, data, executedSteps, result.Errors, cancellationToken);
            }
            return result.Errors;
        }

        executedSteps.Add(stepName);
        return Result.Success;
    }

    private async Task CompleteSagaAsync(
        SagaState sagaState,
        CreateShipmentSagaData data,
        CancellationToken cancellationToken)
    {
        sagaState.Status = SagaStatus.Completed;
        sagaState.CompletedAt = DateTime.UtcNow;
        await sagaRepository.UpdateAsync(sagaState, cancellationToken);

        logger.LogInformation("CreateShipment saga {SagaId} completed successfully for order {OrderId}", 
            sagaState.Id, data.Request.OrderId);
    }

    private async Task CompensateAndFailAsync(
        SagaState sagaState,
        CreateShipmentSagaData data,
        List<string> executedSteps,
        List<Error> errors,
        CancellationToken cancellationToken)
    {
        logger.LogWarning("Starting compensation for saga {SagaId}", sagaState.Id);

        sagaState.Status = SagaStatus.Compensating;
        await sagaRepository.UpdateAsync(sagaState, cancellationToken);

        // Compensate in reverse order
        executedSteps.Reverse();

        foreach (var stepName in executedSteps)
        {
            try
            {
                logger.LogInformation("Compensating step: {StepName}", stepName);

                Result<Success> compensationResult = stepName switch
                {
                    "CreateCarrierShipment" => await createCarrierShipmentHandler.CompensateAsync(data, cancellationToken),
                    "DecreaseStock" => await decreaseStockHandler.CompensateAsync(data, cancellationToken),
                    "CreateShipment" => await createShipmentHandler.CompensateAsync(data, cancellationToken),
                    _ => Result.Success
                };

                if (compensationResult.IsError)
                {
                    logger.LogError("Compensation failed for step {StepName}: {@Errors}", 
                        stepName, compensationResult.Errors);
                }
                else
                {
                    logger.LogInformation("Compensation successful for step {StepName}", stepName);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during compensation of step {StepName}", stepName);
            }
        }

        sagaState.Status = SagaStatus.Compensated;
        sagaState.CompletedAt = DateTime.UtcNow;
        sagaState.Error = System.Text.Json.JsonSerializer.Serialize(errors);
        await sagaRepository.UpdateAsync(sagaState, cancellationToken);

        logger.LogWarning("Compensation completed for saga {SagaId}", sagaState.Id);
    }

    private async Task FailSagaAsync(
        SagaState sagaState,
        List<Error> errors,
        CancellationToken cancellationToken)
    {
        sagaState.Status = SagaStatus.Failed;
        sagaState.Error = System.Text.Json.JsonSerializer.Serialize(errors);
        sagaState.CompletedAt = DateTime.UtcNow;
        await sagaRepository.UpdateAsync(sagaState, cancellationToken);
    }
}

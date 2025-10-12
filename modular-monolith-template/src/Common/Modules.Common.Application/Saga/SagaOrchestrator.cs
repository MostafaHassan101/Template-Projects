using System.Text.Json;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Results;
using Modules.Common.Domain.Saga;

namespace Modules.Common.Application.Saga;


// Orchestrates saga execution with automatic compensation on failure
public class SagaOrchestrator<TData>(
    ISagaRepository sagaRepository,
    ILogger<SagaOrchestrator<TData>> logger) : ISagaOrchestrator<TData>
{
    public async Task<Result<Success>> ExecuteAsync(
        ISaga<TData> saga,
        TData data,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var sagaState = await InitializeSagaAsync(saga, data, correlationId, cancellationToken);
        var executedSteps = new List<ISagaStep<TData>>();

        try
        {
            sagaState.Status = SagaStatus.InProgress;
            await sagaRepository.UpdateAsync(sagaState, cancellationToken);

            var result = await ExecuteStepsAsync(saga, data, sagaState, executedSteps, cancellationToken);
            if (result.IsError)
            {
                return result.Errors;
            }

            await CompleteSagaAsync(saga, sagaState, cancellationToken);
            return Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in saga {SagaType} with ID {SagaId}", 
                saga.SagaType, sagaState.Id);

            await CompensateAsync(data, sagaState, executedSteps, cancellationToken);

            sagaState.Status = SagaStatus.Failed;
            sagaState.Error = ex.Message;
            await sagaRepository.UpdateAsync(sagaState, cancellationToken);

            return Error.Unexpected("Saga.ExecutionFailed", $"Saga execution failed: {ex.Message}");
        }
    }

    private async Task<SagaState> InitializeSagaAsync(
        ISaga<TData> saga,
        TData data,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var sagaState = new SagaState
        {
            Id = Guid.NewGuid(),
            SagaType = saga.SagaType,
            CorrelationId = correlationId,
            Status = SagaStatus.Started,
            CurrentStep = "Initial",
            Payload = JsonSerializer.Serialize(data),
            CreatedAt = DateTime.UtcNow
        };

        await sagaRepository.AddAsync(sagaState, cancellationToken);
        logger.LogInformation("Started saga {SagaType} with ID {SagaId}", saga.SagaType, sagaState.Id);

        return sagaState;
    }

    private async Task<Result<Success>> ExecuteStepsAsync(
        ISaga<TData> saga,
        TData data,
        SagaState sagaState,
        List<ISagaStep<TData>> executedSteps,
        CancellationToken cancellationToken)
    {
        foreach (var step in saga.Steps)
        {
            sagaState.CurrentStep = step.StepName;
            await sagaRepository.UpdateAsync(sagaState, cancellationToken);

            logger.LogInformation("Executing saga step {StepName} for saga {SagaId}", step.StepName, sagaState.Id);

            var result = await step.ExecuteAsync(data, cancellationToken);

            if (result.IsError)
            {
                logger.LogError("Saga step {StepName} failed for saga {SagaId}: {@Errors}", 
                    step.StepName, sagaState.Id, result.Errors);

                await CompensateAsync(data, sagaState, executedSteps, cancellationToken);
                
                sagaState.Status = SagaStatus.Failed;
                sagaState.Error = JsonSerializer.Serialize(result.Errors);
                await sagaRepository.UpdateAsync(sagaState, cancellationToken);

                return result.Errors;
            }

            executedSteps.Add(step);
            logger.LogInformation("Saga step {StepName} completed successfully for saga {SagaId}", 
                step.StepName, sagaState.Id);
        }

        return Result.Success;
    }

    private async Task CompleteSagaAsync(
        ISaga<TData> saga,
        SagaState sagaState,
        CancellationToken cancellationToken)
    {
        sagaState.Status = SagaStatus.Completed;
        sagaState.CompletedAt = DateTime.UtcNow;
        await sagaRepository.UpdateAsync(sagaState, cancellationToken);

        logger.LogInformation("Saga {SagaType} with ID {SagaId} completed successfully", 
            saga.SagaType, sagaState.Id);
    }

    private async Task CompensateAsync(
        TData data,
        SagaState sagaState,
        List<ISagaStep<TData>> executedSteps,
        CancellationToken cancellationToken)
    {
        logger.LogWarning("Starting compensation for saga {SagaId}", sagaState.Id);

        sagaState.Status = SagaStatus.Compensating;
        await sagaRepository.UpdateAsync(sagaState, cancellationToken);

        // Compensate in reverse order
        executedSteps.Reverse();

        foreach (var step in executedSteps)
        {
            try
            {
                logger.LogInformation("Compensating saga step {StepName} for saga {SagaId}", 
                    step.StepName, sagaState.Id);

                var result = await step.CompensateAsync(data, cancellationToken);

                if (result.IsError)
                {
                    logger.LogError("Compensation failed for step {StepName} in saga {SagaId}: {@Errors}", 
                        step.StepName, sagaState.Id, result.Errors);
                    // Continue with other compensations even if one fails
                }
                else
                {
                    logger.LogInformation("Compensation successful for step {StepName} in saga {SagaId}", 
                        step.StepName, sagaState.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during compensation of step {StepName} in saga {SagaId}", 
                    step.StepName, sagaState.Id);
                // Continue with other compensations
            }
        }

        sagaState.Status = SagaStatus.Compensated;
        sagaState.CompletedAt = DateTime.UtcNow;
        await sagaRepository.UpdateAsync(sagaState, cancellationToken);

        logger.LogWarning("Compensation completed for saga {SagaId}", sagaState.Id);
    }
}

public interface ISagaOrchestrator<TData>
{
    Task<Result<Success>> ExecuteAsync(
        ISaga<TData> saga,
        TData data,
        string correlationId,
        CancellationToken cancellationToken = default);
}

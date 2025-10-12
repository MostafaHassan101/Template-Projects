using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modules.Common.Application.Outbox;
using Modules.Common.Domain.Events;

namespace Modules.Common.Infrastructure.Outbox;

/// <summary>
/// Background service that processes outbox messages
/// </summary>
public class OutboxProcessor(
    IServiceProvider serviceProvider,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(10);
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_processingInterval, stoppingToken);
        }

        logger.LogInformation("Outbox Processor stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var messages = await repository.GetUnprocessedMessagesAsync(BatchSize, cancellationToken);
        if (messages.Count == 0)
        {
            return;
        }
        logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                // Deserialize event
                var eventType = Type.GetType(message.EventType);
                if (eventType == null)
                {
                    logger.LogError("Could not resolve event type {EventType}", message.EventType);
                    await repository.MarkAsFailedAsync(message.Id, $"Could not resolve event type: {message.EventType}", cancellationToken);
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.Payload, eventType);
                if (@event == null)
                {
                    logger.LogError("Could not deserialize event {EventType}", message.EventType);
                    await repository.MarkAsFailedAsync(message.Id, "Could not deserialize event", cancellationToken);
                    continue;
                }
                // Publish event using reflection
                var publishMethod = typeof(IEventPublisher)
                    .GetMethod(nameof(IEventPublisher.PublishAsync))!
                    .MakeGenericMethod(eventType);

                var publishTask = (Task)publishMethod.Invoke(eventPublisher, [@event, cancellationToken])!;
                await publishTask;

                await repository.MarkAsProcessedAsync(message.Id, cancellationToken);
                logger.LogInformation("Successfully processed outbox message {MessageId}", message.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox message {MessageId}", message.Id);

                if (message.CanRetry)
                {
                    await repository.IncrementRetryCountAsync(message.Id, cancellationToken);
                    logger.LogWarning("Incremented retry count for message {MessageId} to {RetryCount}", 
                        message.Id, message.RetryCount + 1);
                }
                else
                {
                    await repository.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
                    logger.LogError("Message {MessageId} exceeded max retries and marked as failed", message.Id);
                }
            }
        }
    }
}

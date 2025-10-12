using System.Text.Json;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.Events;
using Modules.Common.Domain.Outbox;

namespace Modules.Common.Application.Outbox;

/// <summary>
/// Event publisher that stores events in outbox instead of publishing immediately
/// </summary>
public class OutboxEventPublisher(
    IOutboxRepository outboxRepository,
    ILogger<OutboxEventPublisher> logger) : IEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) 
        where TEvent : IEvent
    {
        var eventType = @event.GetType();
        logger.LogDebug("Storing event {EventType} in outbox", eventType.Name);

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = $"{eventType.FullName}, {eventType.Assembly.GetName().Name}",
            Payload = JsonSerializer.Serialize(@event, eventType),
            CreatedAt = DateTime.UtcNow
        };

        await outboxRepository.AddAsync(outboxMessage, cancellationToken);

        logger.LogInformation("Event {EventType} stored in outbox with ID {MessageId}", 
            eventType.Name, outboxMessage.Id);
    }
}

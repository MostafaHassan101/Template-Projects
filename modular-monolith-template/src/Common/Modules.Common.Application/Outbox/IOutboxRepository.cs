using Modules.Common.Domain.Outbox;

namespace Modules.Common.Application.Outbox;

/// <summary>
/// Repository for managing outbox messages
/// </summary>
public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
    Task IncrementRetryCountAsync(Guid messageId, CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using Modules.Common.Application.Outbox;
using Modules.Common.Domain.Outbox;
using Modules.Shipments.Infrastructure.Database;

namespace Modules.Shipments.Infrastructure.Outbox;

internal sealed class OutboxRepository(ShipmentsDbContext context) : IOutboxRepository
{
    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await context.OutboxMessages.AddAsync(message, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < m.MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await context.OutboxMessages.FindAsync([messageId], cancellationToken);
        if (message != null)
        {
            message.ProcessedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var message = await context.OutboxMessages.FindAsync([messageId], cancellationToken);
        if (message != null)
        {
            message.Error = error;
            message.ProcessedAt = DateTime.UtcNow; // Mark as processed to prevent retries
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task IncrementRetryCountAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await context.OutboxMessages.FindAsync([messageId], cancellationToken);
        if (message != null)
        {
            message.RetryCount++;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}

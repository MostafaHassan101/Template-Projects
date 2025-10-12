using Microsoft.EntityFrameworkCore;
using Modules.Common.Application.Saga;
using Modules.Common.Domain.Saga;
using Modules.Shipments.Infrastructure.Database;

namespace Modules.Shipments.Infrastructure.Saga;

internal sealed class SagaRepository(ShipmentsDbContext context) : ISagaRepository
{
    public async Task AddAsync(SagaState sagaState, CancellationToken cancellationToken = default)
    {
        await context.SagaStates.AddAsync(sagaState, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SagaState?> GetByIdAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        return await context.SagaStates
            .FirstOrDefaultAsync(s => s.Id == sagaId, cancellationToken);
    }

    public async Task<SagaState?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        return await context.SagaStates
            .FirstOrDefaultAsync(s => s.CorrelationId == correlationId, cancellationToken);
    }

    public async Task UpdateAsync(SagaState sagaState, CancellationToken cancellationToken = default)
    {
        context.SagaStates.Update(sagaState);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<SagaState>> GetPendingSagasAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await context.SagaStates
            .Where(s => s.Status == SagaStatus.InProgress || s.Status == SagaStatus.Compensating)
            .OrderBy(s => s.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}

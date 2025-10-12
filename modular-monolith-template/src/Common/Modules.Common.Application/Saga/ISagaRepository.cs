using Modules.Common.Domain.Saga;

namespace Modules.Common.Application.Saga;

/// <summary>
/// Repository for managing saga state
/// </summary>
public interface ISagaRepository
{
    Task AddAsync(SagaState sagaState, CancellationToken cancellationToken = default);
    Task<SagaState?> GetByIdAsync(Guid sagaId, CancellationToken cancellationToken = default);
    Task<SagaState?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
    Task UpdateAsync(SagaState sagaState, CancellationToken cancellationToken = default);
    Task<List<SagaState>> GetPendingSagasAsync(int batchSize, CancellationToken cancellationToken = default);
}

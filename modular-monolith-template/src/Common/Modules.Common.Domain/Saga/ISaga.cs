using Modules.Common.Domain.Results;

namespace Modules.Common.Domain.Saga;

/// <summary>
/// Represents a saga step in a distributed transaction
/// </summary>
public interface ISagaStep<TData>
{
    string StepName { get; }
    Task<Result<Success>> ExecuteAsync(TData data, CancellationToken cancellationToken);
    Task<Result<Success>> CompensateAsync(TData data, CancellationToken cancellationToken);
}

/// <summary>
/// Represents a saga orchestrator for managing distributed transactions
/// </summary>
public interface ISaga<TData>
{
    string SagaType { get; }
    List<ISagaStep<TData>> Steps { get; }
}

namespace Modules.Common.Domain.Saga;

/// <summary>
/// Represents the state of a saga execution
/// </summary>
public enum SagaStatus
{
    Started,
    InProgress,
    Completed,
    Compensating,
    Compensated,
    Failed
}

/// <summary>
/// Represents a saga instance tracking distributed transaction state
/// </summary>
public class SagaState
{
    public Guid Id { get; set; }
    public string SagaType { get; set; } = null!;
    public string CorrelationId { get; set; } = null!;
    public SagaStatus Status { get; set; }
    public string CurrentStep { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public string? CompensationData { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
    
    public bool IsCompleted => Status == SagaStatus.Completed;
    public bool IsFailed => Status == SagaStatus.Failed;
    public bool NeedsCompensation => Status == SagaStatus.Compensating;
}

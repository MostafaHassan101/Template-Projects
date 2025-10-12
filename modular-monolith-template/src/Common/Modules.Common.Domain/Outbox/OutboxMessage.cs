namespace Modules.Common.Domain.Outbox;

/// <summary>
/// Represents an outbox message for reliable event publishing
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    
    public bool IsProcessed => ProcessedAt.HasValue;
    public bool CanRetry => RetryCount < MaxRetries;
}

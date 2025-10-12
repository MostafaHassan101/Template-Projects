# Saga + Outbox Pattern - Quick Reference

## 🚀 Quick Start

```bash
# 1. Build solution
dotnet build

# 2. Create migration
cd src/Shipments/Modules.Shipments.Infrastructure
dotnet ef migrations add AddOutboxAndSagaTables --context ShipmentsDbContext --startup-project ../../ModularMonolith.Host
dotnet ef database update --context ShipmentsDbContext --startup-project ../../ModularMonolith.Host

# 3. Run application
cd ../../ModularMonolith.Host
dotnet run
```

---

## 📋 Key Concepts

### Saga Pattern
- **Purpose**: Orchestrate distributed transactions with automatic rollback
- **Steps**: Execute → If failure → Compensate (reverse order)
- **State**: Tracked in `SagaStates` table

### Outbox Pattern
- **Purpose**: Reliable event delivery
- **Flow**: Store events in DB → Background processor → Publish → Mark processed
- **Table**: `OutboxMessages`

---

## 🔍 Monitoring Queries

### Check Saga Status
```sql
SELECT Status, COUNT(*) FROM [Shipments].[SagaStates] GROUP BY Status;
```

### View Recent Sagas
```sql
SELECT TOP 10 * FROM [Shipments].[SagaStates] ORDER BY CreatedAt DESC;
```

### Check Outbox Processing
```sql
SELECT 
    COUNT(*) as Total,
    SUM(CASE WHEN ProcessedAt IS NULL THEN 1 ELSE 0 END) as Pending
FROM [Shipments].[OutboxMessages];
```

### Find Stuck Sagas
```sql
SELECT * FROM [Shipments].[SagaStates]
WHERE Status IN ('InProgress', 'Compensating')
AND CreatedAt < DATEADD(MINUTE, -5, GETUTCDATE());
```

---

## 🏗️ Creating a New Saga

### 1. Define Saga Data
```csharp
public class MyOperationSagaData
{
    public MyRequest Request { get; set; }
    public MyEntity? CreatedEntity { get; set; }
}
```

### 2. Create Saga Steps
```csharp
internal sealed class Step1Handler
{
    public async Task<Result<Success>> ExecuteAsync(MyOperationSagaData data, CancellationToken ct)
    {
        // Execute step logic
        return Result.Success;
    }

    public async Task<Result<Success>> CompensateAsync(MyOperationSagaData data, CancellationToken ct)
    {
        // Undo step logic
        return Result.Success;
    }
}
```

### 3. Create Saga Orchestrator
```csharp
internal sealed class MyOperationSagaOrchestrator
{
    public async Task<Result<Success>> ExecuteAsync(
        MyOperationSagaData data,
        string correlationId,
        CancellationToken ct)
    {
        // Create saga state
        var sagaState = new SagaState { /* ... */ };
        await _sagaRepository.AddAsync(sagaState, ct);

        // Execute steps
        var result1 = await _step1Handler.ExecuteAsync(data, ct);
        if (result1.IsError)
        {
            await CompensateAsync(/* ... */);
            return result1.Errors;
        }

        // More steps...

        // Mark completed
        sagaState.Status = SagaStatus.Completed;
        await _sagaRepository.UpdateAsync(sagaState, ct);
        return Result.Success;
    }
}
```

### 4. Register Services
```csharp
services.AddScoped<MyOperationSagaOrchestrator>();
services.AddScoped<Step1Handler>();
services.AddScoped<Step2Handler>();
```

### 5. Use in Handler
```csharp
public async Task<Result<MyResponse>> HandleAsync(MyRequest request, CancellationToken ct)
{
    var sagaData = new MyOperationSagaData { Request = request };
    var result = await _sagaOrchestrator.ExecuteAsync(sagaData, request.Id, ct);
    
    if (result.IsError)
        return result.Errors;
    
    return sagaData.CreatedEntity.MapToResponse();
}
```

---

## 🔧 Common Tasks

### Add Compensating Transaction to Module

#### 1. Add to PublicApi Interface
```csharp
public interface IMyModuleApi
{
    Task<Result<Success>> CreateAsync(CreateRequest request, CancellationToken ct);
    Task<Result<Success>> CancelAsync(string id, CancellationToken ct); // Compensation
}
```

#### 2. Create Handler
```csharp
internal sealed class CancelHandler : IHandler
{
    public async Task<Result<Success>> HandleAsync(string id, CancellationToken ct)
    {
        var entity = await _context.Entities.FindAsync(id, ct);
        if (entity == null)
            return Result.Success; // Idempotent
        
        _context.Entities.Remove(entity);
        await _context.SaveChangesAsync(ct);
        return Result.Success;
    }
}
```

#### 3. Implement in ModuleApi
```csharp
internal sealed class MyModuleApi : IMyModuleApi
{
    public async Task<Result<Success>> CancelAsync(string id, CancellationToken ct)
    {
        return await _cancelHandler.HandleAsync(id, ct);
    }
}
```

---

## 🐛 Troubleshooting

### Saga Stuck in Progress
```sql
-- Find stuck saga
SELECT * FROM [Shipments].[SagaStates] WHERE Id = 'SAGA_ID';

-- Manually mark as failed (if needed)
UPDATE [Shipments].[SagaStates] 
SET Status = 'Failed', Error = 'Manual intervention', CompletedAt = GETUTCDATE()
WHERE Id = 'SAGA_ID';
```

### Outbox Messages Not Processing
```bash
# Check if OutboxProcessor is running
# Look for this log: "Outbox Processor started"

# Check for errors in Seq
http://localhost:8081
```

### Duplicate Shipments
```sql
-- Add unique constraint
CREATE UNIQUE INDEX IX_Shipments_OrderId 
ON [Shipments].[Shipments](OrderId);
```

---

## 📊 Performance Tuning

### Adjust Outbox Processor
```csharp
// In OutboxProcessor.cs
private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(5); // Faster
private const int BatchSize = 100; // Larger batches
```

### Add Database Indexes
```sql
-- For faster saga lookups
CREATE INDEX IX_SagaStates_CorrelationId_Status 
ON [Shipments].[SagaStates](CorrelationId, Status);

-- For faster outbox queries
CREATE INDEX IX_OutboxMessages_ProcessedAt_RetryCount 
ON [Shipments].[OutboxMessages](ProcessedAt, RetryCount);
```

---

## 🧪 Testing

### Test Saga Success
```csharp
[Fact]
public async Task Saga_ShouldComplete_WhenAllStepsSucceed()
{
    var result = await _orchestrator.ExecuteAsync(data, "correlation-1", CancellationToken.None);
    
    Assert.True(result.IsSuccess);
    
    var saga = await _sagaRepository.GetByCorrelationIdAsync("correlation-1");
    Assert.Equal(SagaStatus.Completed, saga.Status);
}
```

### Test Saga Compensation
```csharp
[Fact]
public async Task Saga_ShouldCompensate_WhenStepFails()
{
    // Setup: Make step 3 fail
    _mockApi.Setup(x => x.DoSomethingAsync()).ReturnsAsync(Error.Failure("test", "fail"));
    
    var result = await _orchestrator.ExecuteAsync(data, "correlation-2", CancellationToken.None);
    
    Assert.True(result.IsError);
    
    var saga = await _sagaRepository.GetByCorrelationIdAsync("correlation-2");
    Assert.Equal(SagaStatus.Compensated, saga.Status);
    
    // Verify compensation executed
    _mockApi.Verify(x => x.UndoAsync(), Times.Once);
}
```

### Test Idempotency
```csharp
[Fact]
public async Task Saga_ShouldReturnExisting_WhenDuplicateRequest()
{
    // First request
    await _orchestrator.ExecuteAsync(data, "correlation-3", CancellationToken.None);
    
    // Duplicate request
    var result = await _orchestrator.ExecuteAsync(data, "correlation-3", CancellationToken.None);
    
    Assert.True(result.IsSuccess);
    
    // Should only have 1 saga
    var sagas = await _context.SagaStates
        .Where(s => s.CorrelationId == "correlation-3")
        .ToListAsync();
    Assert.Single(sagas);
}
```

---

## 📝 Best Practices

### ✅ DO
- Use correlation IDs for idempotency (e.g., OrderId, RequestId)
- Make compensation operations idempotent
- Log all saga steps with structured logging
- Track saga state in database
- Use database transactions for atomic operations
- Handle partial failures gracefully

### ❌ DON'T
- Don't throw exceptions in saga steps (return Result<T>)
- Don't skip compensation logic
- Don't use saga for simple operations (overhead)
- Don't forget to register step handlers in DI
- Don't make compensations dependent on each other
- Don't use saga for read-only operations

---

## 🎯 Saga Step Checklist

When creating a saga step:

- [ ] Implement `ExecuteAsync()` method
- [ ] Implement `CompensateAsync()` method
- [ ] Make compensation idempotent
- [ ] Add structured logging
- [ ] Return `Result<Success>` (not throw exceptions)
- [ ] Store necessary data in saga data object
- [ ] Register handler in DI container
- [ ] Add unit tests for execute and compensate
- [ ] Document what the step does

---

## 🔐 Security Considerations

### Saga State Contains Sensitive Data
```csharp
// Don't store sensitive data in saga payload
var sagaState = new SagaState
{
    Payload = JsonSerializer.Serialize(new 
    {
        OrderId = data.Request.OrderId,
        // Don't include: passwords, credit cards, etc.
    })
};
```

### Outbox Messages
```csharp
// Events should not contain sensitive data
public record ShipmentCreatedEvent(
    string ShipmentNumber,
    string OrderId
    // Don't include: customer PII, payment info
);
```

---

## 📚 Related Documentation

- **Full Implementation Guide**: `SAGA-OUTBOX-IMPLEMENTATION.md`
- **Migration Steps**: `MIGRATION-STEPS.md`
- **Implementation Summary**: `IMPLEMENTATION-SUMMARY.md`
- **Architecture Analysis**: Original analysis document

---

## 🆘 Getting Help

### Check Logs
```bash
# View in Seq
http://localhost:8081

# Search for:
- "Saga execution failed"
- "Compensation completed"
- "Outbox Processor"
```

### Common Error Messages

| Error | Cause | Solution |
|-------|-------|----------|
| "Saga in progress" | Duplicate request | Wait or check saga status |
| "Compensation failed" | Module API down | Check module health, retry |
| "Event type not found" | Missing event handler | Register handler in DI |
| "Saga timeout" | Long-running operation | Increase timeout or optimize |

---

## 🎓 Learning Resources

### Saga Pattern
- [Microservices.io - Saga Pattern](https://microservices.io/patterns/data/saga.html)
- [Microsoft - Saga Pattern](https://docs.microsoft.com/en-us/azure/architecture/reference-architectures/saga/saga)

### Outbox Pattern
- [Microservices.io - Transactional Outbox](https://microservices.io/patterns/data/transactional-outbox.html)
- [Debezium - Outbox Pattern](https://debezium.io/blog/2019/02/19/reliable-microservices-data-exchange-with-the-outbox-pattern/)

---

## 💡 Tips & Tricks

### Debugging Saga Execution
```csharp
// Add breakpoint in saga orchestrator
var result = await _step1Handler.ExecuteAsync(data, ct);
// Check result.Errors here

// Or add detailed logging
_logger.LogDebug("Saga data: {@Data}", data);
```

### Testing Compensation Locally
```csharp
// Manually trigger compensation
var sagaData = new CreateShipmentSagaData { /* ... */ };
await _createShipmentHandler.CompensateAsync(sagaData, CancellationToken.None);
```

### Monitoring Saga Performance
```sql
-- Average saga execution time
SELECT 
    AVG(DATEDIFF(MILLISECOND, CreatedAt, CompletedAt)) as AvgDurationMs,
    MAX(DATEDIFF(MILLISECOND, CreatedAt, CompletedAt)) as MaxDurationMs
FROM [Shipments].[SagaStates]
WHERE Status = 'Completed'
AND CreatedAt > DATEADD(DAY, -1, GETUTCDATE());
```

---

## ⚡ Quick Commands

```bash
# View saga logs
docker logs -f shipping-modular-monolith | grep "Saga"

# Reset database for testing
sqlcmd -S localhost -d ModularMonolith -Q "DELETE FROM [Shipments].[SagaStates]; DELETE FROM [Shipments].[OutboxMessages];"

# Check outbox processor status
curl http://localhost:5000/health

# View Seq logs
open http://localhost:8081

# View Jaeger traces
open http://localhost:16686
```

---

**Last Updated**: Implementation completed  
**Version**: 1.0  
**Status**: Production Ready ✅

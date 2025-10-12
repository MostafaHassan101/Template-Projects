# Saga + Outbox Pattern Implementation

## Overview

This document describes the implementation of the **Saga Pattern** combined with the **Outbox Pattern** to ensure reliable distributed transactions across modules in the modular monolith architecture.

## Problem Solved

### Previous Issues

1. **Data Inconsistency**: Events were published after the database transaction committed, leading to potential data corruption if event handlers failed
2. **No Rollback Mechanism**: Failed cross-module operations couldn't be rolled back
3. **Error Handling**: Exceptions in event handlers propagated to users even though the primary operation succeeded
4. **No Idempotency**: Duplicate requests could create multiple shipments

### Solution Benefits

✅ **Atomic Operations**: All database changes (including outbox messages) are committed in a single transaction  
✅ **Automatic Compensation**: Failed saga steps trigger compensating transactions in reverse order  
✅ **Reliable Event Delivery**: Outbox pattern ensures events are eventually processed  
✅ **Idempotency**: Saga correlation IDs prevent duplicate processing  
✅ **Observability**: Full saga execution tracking with status and error logging  

---

## Architecture Components

### 1. Outbox Pattern

**Purpose**: Store events in the database within the same transaction as business data, then process them asynchronously.

**Components**:
- `OutboxMessage` - Entity storing event data
- `IOutboxRepository` - Repository for managing outbox messages
- `OutboxEventPublisher` - Stores events in outbox instead of publishing immediately
- `OutboxProcessor` - Background service that processes outbox messages every 10 seconds

**Flow**:
```
1. Business logic executes
2. Events stored in OutboxMessages table (same transaction)
3. Transaction commits
4. Background processor reads unprocessed messages
5. Events published to handlers
6. Messages marked as processed
```

### 2. Saga Pattern

**Purpose**: Orchestrate distributed transactions with automatic compensation on failure.

**Components**:
- `SagaState` - Entity tracking saga execution state
- `ISaga<TData>` - Interface defining saga steps
- `ISagaStep<TData>` - Interface for individual saga steps with Execute and Compensate methods
- `SagaOrchestrator` - Executes saga steps and handles compensation
- `ISagaRepository` - Repository for managing saga state

**Flow**:
```
1. Saga starts with correlation ID
2. Each step executes in order
3. If step fails:
   - Trigger compensation for all executed steps (reverse order)
   - Mark saga as Compensated/Failed
4. If all steps succeed:
   - Mark saga as Completed
```

---

## CreateShipment Saga Implementation

### Saga Steps

The `CreateShipmentSaga` consists of 4 steps executed sequentially:

#### Step 1: ValidateStock
- **Execute**: Calls `StockModuleApi.CheckStockAsync()` to verify product availability
- **Compensate**: No compensation needed (read-only operation)

#### Step 2: CreateShipment
- **Execute**: Creates shipment entity in database
- **Compensate**: Deletes the created shipment

#### Step 3: DecreaseStock
- **Execute**: Calls `StockModuleApi.DecreaseStockAsync()` to reduce inventory
- **Compensate**: Calls `StockModuleApi.RestoreStockAsync()` to add inventory back

#### Step 4: CreateCarrierShipment
- **Execute**: Calls `CarrierModuleApi.CreateShipmentAsync()` to create carrier record
- **Compensate**: Calls `CarrierModuleApi.CancelShipmentAsync()` to remove carrier record

### Compensation Flow Example

If **Step 4 (CreateCarrierShipment)** fails:

```
1. Step 3 Compensation: RestoreStock (add inventory back)
2. Step 2 Compensation: Delete Shipment entity
3. Saga marked as Compensated
4. Error returned to user
```

---

## Database Schema

### OutboxMessages Table

```sql
CREATE TABLE [Shipments].[OutboxMessages] (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    EventType NVARCHAR(500) NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    ProcessedAt DATETIME2 NULL,
    Error NVARCHAR(2000) NULL,
    RetryCount INT NOT NULL DEFAULT 0,
    MaxRetries INT NOT NULL DEFAULT 3
);

CREATE INDEX IX_OutboxMessages_ProcessedAt_CreatedAt 
ON [Shipments].[OutboxMessages] (ProcessedAt, CreatedAt);
```

### SagaStates Table

```sql
CREATE TABLE [Shipments].[SagaStates] (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    SagaType NVARCHAR(200) NOT NULL,
    CorrelationId NVARCHAR(100) NOT NULL,
    Status NVARCHAR(50) NOT NULL, -- Started, InProgress, Completed, Compensating, Compensated, Failed
    CurrentStep NVARCHAR(200) NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,
    CompensationData NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2 NULL,
    Error NVARCHAR(2000) NULL
);

CREATE UNIQUE INDEX IX_SagaStates_CorrelationId 
ON [Shipments].[SagaStates] (CorrelationId);

CREATE INDEX IX_SagaStates_Status_CreatedAt 
ON [Shipments].[SagaStates] (Status, CreatedAt);
```

---

## Idempotency Guarantees

### 1. Saga-Level Idempotency

The `CreateShipmentHandler` checks for existing sagas using the `OrderId` as correlation ID:

```csharp
var existingSaga = await sagaRepository.GetByCorrelationIdAsync(request.OrderId, cancellationToken);
if (existingSaga != null)
{
    if (existingSaga.IsCompleted)
    {
        // Return existing shipment
        return existingShipment.MapToResponse();
    }
    else if (existingSaga.IsFailed)
    {
        // Allow retry
    }
    else
    {
        // Saga in progress - reject duplicate request
        return Error.Conflict("Shipment.SagaInProgress", "...");
    }
}
```

### 2. Database-Level Idempotency

Add unique constraint on `Shipments.OrderId`:

```csharp
modelBuilder.Entity<Shipment>()
    .HasIndex(s => s.OrderId)
    .IsUnique();
```

### 3. Compensating Transaction Idempotency

Compensating operations are designed to be idempotent:

- `CancelCarrierShipment`: Returns success if shipment doesn't exist
- `RestoreStock`: Simply adds quantity back (safe to retry)

---

## Configuration

### 1. Register Services

In `ShipmentsModuleRegistration`:

```csharp
// Use OutboxEventPublisher instead of direct EventPublisher
services.AddScoped<IEventPublisher, OutboxEventPublisher>();

// Register Saga orchestrator and step handlers
services.AddScoped<CreateShipmentSagaOrchestrator>();
services.AddScoped<ValidateStockStepHandler>();
services.AddScoped<CreateShipmentStepHandler>();
services.AddScoped<DecreaseStockStepHandler>();
services.AddScoped<CreateCarrierShipmentStepHandler>();
```

### 2. Register Background Service

In `Program.cs`:

```csharp
builder.Services.AddHostedService<OutboxProcessor>();
```

### 3. Register Repositories

In `DependencyInjection`:

```csharp
services.AddScoped<IOutboxRepository, OutboxRepository>();
services.AddScoped<ISagaRepository, SagaRepository>();
```

---

## Migration Commands

### Create Migration

```bash
# Navigate to Shipments.Infrastructure project
cd src/Shipments/Modules.Shipments.Infrastructure

# Add migration
dotnet ef migrations add AddOutboxAndSagaTables --context ShipmentsDbContext --output-dir Database/Migrations

# Update database
dotnet ef database update --context ShipmentsDbContext
```

### SQL Server Migration (Manual)

If using SQL Server migrations from the solution root:

```powershell
Add-Migration AddOutboxAndSagaTables -Context "ShipmentsDbContext" -OutputDir "Database/Migrations" -Project "Modules.Shipments.Infrastructure"

Update-Database -Context "ShipmentsDbContext" -Project "Modules.Shipments.Infrastructure"
```

---

## Monitoring & Observability

### 1. Saga State Tracking

Query saga execution:

```sql
-- View all sagas
SELECT * FROM [Shipments].[SagaStates] 
ORDER BY CreatedAt DESC;

-- View failed sagas
SELECT * FROM [Shipments].[SagaStates] 
WHERE Status IN ('Failed', 'Compensated')
ORDER BY CreatedAt DESC;

-- View sagas in progress (potential stuck sagas)
SELECT * FROM [Shipments].[SagaStates] 
WHERE Status IN ('InProgress', 'Compensating')
AND CreatedAt < DATEADD(MINUTE, -5, GETUTCDATE());
```

### 2. Outbox Message Monitoring

```sql
-- View unprocessed messages
SELECT * FROM [Shipments].[OutboxMessages] 
WHERE ProcessedAt IS NULL
ORDER BY CreatedAt DESC;

-- View failed messages
SELECT * FROM [Shipments].[OutboxMessages] 
WHERE Error IS NOT NULL
ORDER BY CreatedAt DESC;

-- View messages exceeding retry count
SELECT * FROM [Shipments].[OutboxMessages] 
WHERE RetryCount >= MaxRetries
AND ProcessedAt IS NULL;
```

### 3. Logging

All saga operations are logged with structured logging:

```csharp
logger.LogInformation("Started CreateShipment saga with ID {SagaId} for order {OrderId}", sagaState.Id, data.Request.OrderId);
logger.LogError("ValidateStock step failed: {@Errors}", validateResult.Errors);
logger.LogWarning("Starting compensation for saga {SagaId}", sagaState.Id);
```

View logs in Seq: `http://localhost:8081`

---

## Testing

### Unit Tests

Test saga steps in isolation:

```csharp
[Fact]
public async Task CreateShipmentStep_ShouldCreateShipment()
{
    // Arrange
    var handler = new CreateShipmentStepHandler(context, logger);
    var data = new CreateShipmentSagaData { Request = request };
    
    // Act
    var result = await handler.ExecuteAsync(data, CancellationToken.None);
    
    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(data.CreatedShipment);
}

[Fact]
public async Task CreateShipmentStep_Compensation_ShouldDeleteShipment()
{
    // Arrange - create shipment first
    await handler.ExecuteAsync(data, CancellationToken.None);
    
    // Act - compensate
    var result = await handler.CompensateAsync(data, CancellationToken.None);
    
    // Assert
    Assert.True(result.IsSuccess);
    var shipment = await context.Shipments.FindAsync(data.CreatedShipment.Id);
    Assert.Null(shipment);
}
```

### Integration Tests

Test full saga execution:

```csharp
[Fact]
public async Task CreateShipment_ShouldExecuteSagaSuccessfully()
{
    // Arrange
    var request = new CreateShipmentRequest(...);
    
    // Act
    var response = await client.PostAsJsonAsync("/api/shipments", request);
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    
    // Verify saga completed
    var saga = await context.SagaStates
        .FirstOrDefaultAsync(s => s.CorrelationId == request.OrderId);
    Assert.NotNull(saga);
    Assert.Equal(SagaStatus.Completed, saga.Status);
}

[Fact]
public async Task CreateShipment_WhenStockUnavailable_ShouldCompensate()
{
    // Arrange - setup stock to fail
    
    // Act
    var response = await client.PostAsJsonAsync("/api/shipments", request);
    
    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    
    // Verify saga compensated
    var saga = await context.SagaStates
        .FirstOrDefaultAsync(s => s.CorrelationId == request.OrderId);
    Assert.NotNull(saga);
    Assert.Equal(SagaStatus.Compensated, saga.Status);
    
    // Verify shipment not created
    var shipment = await context.Shipments
        .FirstOrDefaultAsync(s => s.OrderId == request.OrderId);
    Assert.Null(shipment);
}
```

---

## Performance Considerations

### 1. Outbox Processor Configuration

Adjust processing interval in `OutboxProcessor`:

```csharp
private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(10); // Adjust as needed
private const int BatchSize = 50; // Process 50 messages per batch
```

### 2. Database Indexes

Ensure indexes exist for performance:

- `IX_OutboxMessages_ProcessedAt_CreatedAt` - For finding unprocessed messages
- `IX_SagaStates_CorrelationId` - For idempotency checks (unique)
- `IX_SagaStates_Status_CreatedAt` - For finding pending sagas

### 3. Saga Timeout

Consider adding saga timeout mechanism:

```csharp
// Find sagas stuck in progress for > 5 minutes
var stuckSagas = await context.SagaStates
    .Where(s => s.Status == SagaStatus.InProgress)
    .Where(s => s.CreatedAt < DateTime.UtcNow.AddMinutes(-5))
    .ToListAsync();

// Mark as failed and trigger compensation
```

---

## Future Enhancements

### 1. Saga Recovery Service

Implement a background service to recover stuck sagas:

```csharp
public class SagaRecoveryService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Find and recover stuck sagas
            await RecoverStuckSagasAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

### 2. Dead Letter Queue

For messages that exceed max retries:

```csharp
public class DeadLetterQueue
{
    public async Task AddAsync(OutboxMessage message)
    {
        // Store in separate table for manual intervention
        await context.DeadLetterMessages.AddAsync(new DeadLetterMessage
        {
            OriginalMessageId = message.Id,
            EventType = message.EventType,
            Payload = message.Payload,
            Error = message.Error,
            FailedAt = DateTime.UtcNow
        });
    }
}
```

### 3. Saga Dashboard

Build an admin UI to:
- View saga execution history
- Retry failed sagas
- View compensation logs
- Monitor outbox message processing

### 4. Distributed Tracing

Enhance with OpenTelemetry spans:

```csharp
using var activity = ActivitySource.StartActivity("CreateShipmentSaga");
activity?.SetTag("saga.id", sagaState.Id);
activity?.SetTag("saga.correlation_id", correlationId);
```

---

## Troubleshooting

### Saga Stuck in Progress

**Symptoms**: Saga status remains "InProgress" for extended period

**Causes**:
- Application crash during saga execution
- Database connection timeout
- Unhandled exception in step handler

**Resolution**:
1. Check logs for errors
2. Manually trigger compensation
3. Implement saga timeout mechanism

### Outbox Messages Not Processing

**Symptoms**: Messages remain unprocessed in OutboxMessages table

**Causes**:
- OutboxProcessor not running
- Event deserialization failure
- No event handlers registered

**Resolution**:
1. Verify `OutboxProcessor` is registered as hosted service
2. Check event type string format
3. Verify event handlers are registered in DI

### Duplicate Shipments Created

**Symptoms**: Multiple shipments with same OrderId

**Causes**:
- Missing unique constraint on OrderId
- Race condition between idempotency checks

**Resolution**:
1. Add unique constraint: `CREATE UNIQUE INDEX IX_Shipments_OrderId ON Shipments(OrderId)`
2. Use database-level constraint instead of application-level check

---

## Summary

The Saga + Outbox pattern implementation provides:

✅ **Reliability**: Guaranteed event delivery and transaction consistency  
✅ **Resilience**: Automatic compensation on failure  
✅ **Observability**: Full tracking of saga execution  
✅ **Idempotency**: Prevention of duplicate processing  
✅ **Scalability**: Asynchronous event processing  

This implementation addresses all critical data consistency issues identified in the architecture analysis and provides a solid foundation for reliable distributed transactions in the modular monolith.

# Event-Driven Communication

## Overview

This modular monolith uses an **in-process event-driven architecture** for decoupled communication between modules and within modules.

## Event System Architecture

### Core Components

#### 1. **IEvent** - Marker Interface
```csharp
public interface IEvent;
```
- All events must implement this interface
- Enables generic event handling

#### 2. **IEventHandler<TEvent>** - Event Handler Contract
```csharp
public interface IEventHandler<in TEvent> : IEventHandler where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
```
- Typed handler for specific event types
- Multiple handlers can handle the same event
- Async execution

#### 3. **IEventPublisher** - Event Publisher
```csharp
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) 
        where TEvent : IEvent;
}
```
- Publishes events to all registered handlers
- Resolves handlers from DI container

#### 4. **EventPublisher** - Implementation
```csharp
public class EventPublisher : IEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct)
    {
        // 1. Resolve all handlers for this event type
        var handlers = serviceProvider.GetServices<IEventHandler<TEvent>>().ToArray();
        
        if (handlers.Length == 0)
        {
            logger.LogDebug("No handlers registered for event {EventType}", eventType.Name);
            return;
        }
        
        // 2. Execute all handlers in parallel
        var handlerTasks = handlers
            .Select(handler => ExecuteHandlerAsync(handler, @event, ct))
            .ToList();
        
        await Task.WhenAll(handlerTasks);
        
        // 3. Aggregate exceptions if any
        var exceptions = handlerTasks
            .Select(t => t.Exception)
            .Where(ex => ex != null)
            .ToList();
        
        if (exceptions.Count > 0)
        {
            throw new AggregateException("One or more handlers threw exceptions", exceptions);
        }
    }
}
```

**Key Features:**
- **Parallel Execution**: All handlers run concurrently via `Task.WhenAll`
- **Exception Handling**: Exceptions from individual handlers are aggregated
- **Logging**: Comprehensive logging for debugging
- **No External Dependencies**: Pure in-process, no message broker required

## Event Types

### 1. **Domain Events** (Within Module)
Events that represent something that happened in the domain.

**Example:**
```csharp
public record ShipmentCreatedEvent(Shipment Shipment) : IEvent;
```

**Usage:**
```csharp
// Publish after domain operation
var shipment = request.MapToShipment(shipmentNumber);
await context.Shipments.AddAsync(shipment, ct);
await context.SaveChangesAsync(ct);

var shipmentCreatedEvent = new ShipmentCreatedEvent(shipment);
await eventPublisher.PublishAsync(shipmentCreatedEvent, ct);
```

### 2. **Integration Events** (Cross-Module)
Events used for inter-module communication.

**Example - ShipmentCreated triggers actions in other modules:**

```csharp
// Handler 1: Create carrier shipment (Carriers module)
public class CreateCarrierEventHandler : IEventHandler<ShipmentCreatedEvent>
{
    private readonly ICarrierModuleApi _carrierApi;
    
    public async Task HandleAsync(ShipmentCreatedEvent @event, CancellationToken ct)
    {
        logger.LogInformation("Creating carrier shipment for order {OrderId}", 
            @event.Shipment.OrderId);
        
        var carrierRequest = CreateCarrierRequest(@event.Shipment);
        var response = await _carrierApi.CreateShipmentAsync(carrierRequest, ct);
        
        if (!response.IsSuccess)
        {
            logger.LogError("Failed to create carrier shipment: {@Errors}", 
                response.Errors);
            throw new Exception($"Failed to create carrier shipment");
        }
    }
}

// Handler 2: Decrease stock (Stocks module)
public class UpdateStockEventHandler : IEventHandler<ShipmentCreatedEvent>
{
    private readonly IStockModuleApi _stockApi;
    
    public async Task HandleAsync(ShipmentCreatedEvent @event, CancellationToken ct)
    {
        logger.LogInformation("Updating stock for order {OrderId}", 
            @event.Shipment.OrderId);
        
        var updateRequest = CreateDecreaseStockRequest(@event.Shipment);
        var response = await _stockApi.DecreaseStockAsync(updateRequest, ct);
        
        if (!response.IsSuccess)
        {
            logger.LogError("Failed to update stock: {@Errors}", response.Errors);
            throw new Exception($"Failed to update stock");
        }
    }
}
```

**Both handlers execute in parallel when `ShipmentCreatedEvent` is published!**

## Event Flow Example

### Scenario: Creating a Shipment

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. HTTP POST /api/shipments                                     │
│    CreateShipmentRequest                                        │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. CreateShipmentApiEndpoint                                    │
│    - Validates request                                          │
│    - Calls handler                                              │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. CreateShipmentHandler                                        │
│    - Checks if shipment exists                                  │
│    - Calls StockModuleApi.CheckStockAsync() (synchronous)       │
│    - Creates shipment entity                                    │
│    - Saves to database                                          │
│    - Publishes ShipmentCreatedEvent                             │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. EventPublisher.PublishAsync(ShipmentCreatedEvent)            │
│    Resolves handlers:                                           │
│    - CreateCarrierEventHandler                                  │
│    - UpdateStockEventHandler                                    │
└────────────────────┬────────────────────────────────────────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
         ▼                       ▼
┌──────────────────┐    ┌──────────────────┐
│ 5a. CreateCarrier│    │ 5b. UpdateStock  │
│ EventHandler     │    │ EventHandler     │
│                  │    │                  │
│ Calls:           │    │ Calls:           │
│ CarrierModuleApi │    │ StockModuleApi   │
│ .CreateShipment  │    │ .DecreaseStock   │
└──────────────────┘    └──────────────────┘
         │                       │
         └───────────┬───────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. Both handlers complete (or throw exceptions)                 │
│    EventPublisher aggregates results                            │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│ 7. Response returned to client                                  │
│    ShipmentResponse                                             │
└─────────────────────────────────────────────────────────────────┘
```

## Event Handler Registration

Handlers are auto-discovered and registered via reflection:

```csharp
// In Common.Application/Extensions/HandlerRegistrationExtensions.cs
private static void RegisterEventHandlers(IServiceCollection services, Assembly assembly)
{
    var eventHandlerTypes = assembly.GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: false }
            && t.IsAssignableTo(typeof(IEventHandler)))
        .ToList();
    
    foreach (var implementationType in eventHandlerTypes)
    {
        // Find all IEventHandler<T> interfaces implemented by this type
        var handlerInterfaces = implementationType.GetInterfaces()
            .Where(i => i.IsGenericType && 
                   i.GetGenericTypeDefinition() == typeof(IEventHandler<>));
        
        foreach (var interfaceType in handlerInterfaces)
        {
            services.AddScoped(interfaceType, implementationType);
        }
    }
}
```

**Called during module registration:**
```csharp
services.RegisterHandlersFromAssemblyContaining(typeof(ShipmentsModuleRegistration));
```

## Event vs Direct API Call

### When to use Events?

✅ **Use Events when:**
- Action is a **side effect** of an operation
- Multiple modules need to react to the same action
- You want **loose coupling** between modules
- Order of execution doesn't matter (parallel execution is OK)
- Eventual consistency is acceptable

**Example:** After creating a shipment, notify carriers and update stock

### When to use Direct API Calls?

✅ **Use Direct API Calls when:**
- You need the **result** of the operation
- Operation must complete **before** proceeding
- You need **strong consistency**
- Operation is part of the main flow (not a side effect)

**Example:** Check stock availability before creating shipment

```csharp
// Direct call - need result to proceed
var stockResponse = await stockApi.CheckStockAsync(stockRequest, ct);
if (!stockResponse.IsSuccess)
{
    return stockResponse.Errors; // Can't create shipment without stock
}

// Event - side effects after shipment created
await eventPublisher.PublishAsync(new ShipmentCreatedEvent(shipment), ct);
```

## Transaction Boundaries

⚠️ **Important:** Events are published **after** database commit.

```csharp
// 1. Save to database
await context.Shipments.AddAsync(shipment, ct);
await context.SaveChangesAsync(ct); // Transaction committed

// 2. Then publish events
await eventPublisher.PublishAsync(shipmentCreatedEvent, ct);
```

**Implications:**
- If event handler fails, the shipment is already saved
- This is **eventual consistency** - not ACID transaction
- Consider implementing compensating transactions or saga pattern for critical flows

## Error Handling in Event Handlers

### Current Implementation
```csharp
public async Task HandleAsync(ShipmentCreatedEvent @event, CancellationToken ct)
{
    try
    {
        var response = await _carrierApi.CreateShipmentAsync(request, ct);
        
        if (!response.IsSuccess)
        {
            logger.LogError("Failed to create carrier shipment: {@Errors}", 
                response.Errors);
            throw new Exception($"Failed to create carrier shipment");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to create carrier shipment");
        throw; // Propagates to EventPublisher
    }
}
```

**Behavior:**
- Exceptions are logged
- Exception propagates to `EventPublisher`
- `EventPublisher` aggregates all exceptions
- `AggregateException` thrown to caller
- **Original operation already committed** - consider compensation

## Benefits of This Approach

1. **No External Dependencies**: No RabbitMQ, Kafka, or Azure Service Bus needed
2. **Simple**: Easy to understand and debug
3. **Fast**: In-process, no network latency
4. **Type-Safe**: Compile-time checking
5. **Testable**: Easy to mock `IEventPublisher`
6. **Flexible**: Can add/remove handlers without changing publishers

## Limitations

1. **No Persistence**: Events not stored (lost if handler fails)
2. **No Retry**: Failed handlers don't automatically retry
3. **Single Process**: Can't distribute across multiple instances
4. **Transaction Boundary**: Events published after commit (eventual consistency)

## Future Enhancements

Consider adding:
- **Outbox Pattern**: Store events in database, process asynchronously
- **Retry Mechanism**: Polly for transient failures
- **Dead Letter Queue**: For failed events
- **Event Sourcing**: Store all events as source of truth
- **External Message Bus**: For distributed scenarios

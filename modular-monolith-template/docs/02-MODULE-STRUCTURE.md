# Module Structure and Communication

## What Defines a "Module"?

A module is a **logical boundary** representing a business capability with:

### 1. **Physical Structure**
Each module has its own folder with standardized projects:

```
Modules/
├── [ModuleName]/
│   ├── Modules.[ModuleName].Domain/          # Domain entities, value objects, enums
│   ├── Modules.[ModuleName].Infrastructure/  # Database, migrations, EF configurations
│   ├── Modules.[ModuleName].Features/        # Endpoints, handlers, validators
│   └── Modules.[ModuleName].PublicApi/       # Public contracts for inter-module communication
```

**Example - Carriers Module:**
```
Carriers/
├── Modules.Carriers.Domain/
├── Modules.Carriers.Infrastructure/
├── Modules.Carriers.Features/
└── Modules.Carriers.PublicApi/
```

### 2. **Domain Project** (`*.Domain`)
- Contains pure business logic
- No dependencies on infrastructure or frameworks
- Entities, value objects, enums, domain policies
- **Zero external dependencies** (only Common.Domain)

**Example:**
```csharp
// Shipment.cs - Rich domain model
public sealed class Shipment
{
    public Result<Success> Process() { /* State transition logic */ }
    public Result<Success> Dispatch() { /* State transition logic */ }
    public Result<Success> Cancel() { /* Business rules */ }
}
```

### 3. **Infrastructure Project** (`*.Infrastructure`)
- Database context (EF Core)
- Entity configurations (Fluent API)
- Migrations (per-module schema)
- Database migrator implementation
- Infrastructure services

**Key Features:**
- Each module has its own **database schema** (e.g., `shipments`, `carriers`, `stocks`)
- Separate migration history per module
- Independent database evolution

**Example:**
```csharp
public class ShipmentsDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DbConsts.ShipmentsSchemaName); // "shipments"
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShipmentsDbContext).Assembly);
    }
}
```

### 4. **Features Project** (`*.Features`)
- API endpoints (Minimal API)
- Command/Query handlers
- Request/Response DTOs
- Validators (FluentValidation)
- Event handlers
- Internal API implementation

**Organization:**
```
Features/
├── Features/
│   ├── CreateShipment/
│   │   ├── CreateShipment.Endpoint.cs
│   │   ├── CreateShipment.Handler.cs
│   │   ├── CreateShipment.Validator.cs
│   │   ├── CreateShipment.Mapper.cs
│   │   └── Events/
│   │       ├── ShipmentCreatedEvent.cs
│   │       ├── CreateCarrierEventHandler.cs
│   │       └── UpdateStockEventHandler.cs
│   ├── GetShipmentByNumber/
│   └── DispatchShipment/
├── DependencyInjection.cs
└── Tracing/
```

### 5. **PublicApi Project** (`*.PublicApi`) - **Critical for Module Isolation**
- **Contracts only** - interfaces and DTOs
- Defines the module's public surface
- Other modules can only reference this project
- No implementation details exposed

**Example:**
```csharp
// ICarrierModuleApi.cs
public interface ICarrierModuleApi
{
    Task<Result<Success>> CreateShipmentAsync(
        CreateCarrierShipmentRequest request, 
        CancellationToken cancellationToken);
}

// Contracts/CreateCarrierShipmentRequest.cs
public record CreateCarrierShipmentRequest(
    string OrderId,
    Address Address,
    string Carrier,
    string ReceiverEmail,
    List<CarrierShipmentItem> Items);
```

## Module Communication Patterns

### 1. **PublicApi Pattern** (Synchronous)

**How it works:**
1. Module exposes interface in `*.PublicApi` project
2. Implementation lives in `*.Features/InternalApi/`
3. Registered in DI during module startup
4. Other modules inject and call the interface

**Example - Shipments calling Carriers:**

```csharp
// In Carriers.PublicApi (contract)
public interface ICarrierModuleApi
{
    Task<Result<Success>> CreateShipmentAsync(...);
}

// In Carriers.Features/InternalApi (implementation)
internal sealed class CarrierModuleApi : ICarrierModuleApi
{
    public async Task<Result<Success>> CreateShipmentAsync(...)
    {
        return await createShipmentHandler.HandleAsync(request, ct);
    }
}

// In Carriers.Features/DependencyInjection.cs (registration)
services.AddScoped<CarrierModuleApi>();
services.AddScoped<ICarrierModuleApi>(provider =>
{
    var impl = provider.GetRequiredService<CarrierModuleApi>();
    return new TracedCarrierModuleApi(impl); // Decorator for tracing
});

// In Shipments.Features (consumption)
public class CreateCarrierEventHandler : IEventHandler<ShipmentCreatedEvent>
{
    private readonly ICarrierModuleApi _carrierApi;
    
    public async Task HandleAsync(ShipmentCreatedEvent @event, CancellationToken ct)
    {
        var response = await _carrierApi.CreateShipmentAsync(request, ct);
    }
}
```

**Benefits:**
- Type-safe communication
- Compile-time contract verification
- Easy to mock for testing
- Clear module boundaries

### 2. **Event-Driven Pattern** (Asynchronous)

**How it works:**
1. Handler publishes event via `IEventPublisher`
2. Event publisher resolves all `IEventHandler<TEvent>` from DI
3. All handlers execute in parallel
4. Exceptions aggregated and thrown

**Example - ShipmentCreated Event:**

```csharp
// 1. Define event
public record ShipmentCreatedEvent(Shipment Shipment) : IEvent;

// 2. Publish event
public class CreateShipmentHandler : ICreateShipmentHandler
{
    private readonly IEventPublisher _eventPublisher;
    
    public async Task<Result<ShipmentResponse>> HandleAsync(...)
    {
        await context.SaveChangesAsync(ct);
        
        var shipmentCreatedEvent = new ShipmentCreatedEvent(shipment);
        await _eventPublisher.PublishAsync(shipmentCreatedEvent, ct);
        
        return shipment.MapToResponse();
    }
}

// 3. Handle event (multiple handlers can subscribe)
public class CreateCarrierEventHandler : IEventHandler<ShipmentCreatedEvent>
{
    public async Task HandleAsync(ShipmentCreatedEvent @event, CancellationToken ct)
    {
        await _carrierApi.CreateShipmentAsync(...);
    }
}

public class UpdateStockEventHandler : IEventHandler<ShipmentCreatedEvent>
{
    public async Task HandleAsync(ShipmentCreatedEvent @event, CancellationToken ct)
    {
        await _stockApi.DecreaseStockAsync(...);
    }
}
```

**Event Publisher Implementation:**
```csharp
public class EventPublisher : IEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct)
    {
        var handlers = serviceProvider.GetServices<IEventHandler<TEvent>>();
        
        var handlerTasks = handlers
            .Select(handler => ExecuteHandlerAsync(handler, @event, ct))
            .ToList();
        
        await Task.WhenAll(handlerTasks); // Parallel execution
        
        // Aggregate exceptions if any
    }
}
```

## Module Registration

Each module provides an extension method for registration:

```csharp
// In Shipments.Features/DependencyInjection.cs
public static class ShipmentsModuleRegistration
{
    public static IServiceCollection AddShipmentsModule(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        return services
            .AddShipmentsModuleApi()
            .AddShipmentsInfrastructure(configuration);
    }
    
    private static IServiceCollection AddShipmentsModuleApi(this IServiceCollection services)
    {
        services.AddScoped<IEventPublisher, EventPublisher>();
        services.RegisterApiEndpointsFromAssemblyContaining(typeof(ShipmentsModuleRegistration));
        services.RegisterHandlersFromAssemblyContaining(typeof(ShipmentsModuleRegistration));
        services.AddValidatorsFromAssembly(typeof(ShipmentsModuleRegistration).Assembly);
        return services;
    }
}

// In Program.cs
builder.Services
    .AddUsersModule(builder.Configuration)
    .AddShipmentsModule(builder.Configuration)
    .AddCarriersModule(builder.Configuration)
    .AddStocksModule(builder.Configuration);
```

## Module Isolation Rules

### Architecture Tests (NetArchTest)

The solution enforces module boundaries with automated tests:

```csharp
[Fact]
public void ShipmentsModule_ShouldOnlyReference_PublicApiProjects()
{
    var result = Types.InAssemblies(shipmentsAssemblies)
        .Should()
        .NotHaveDependencyOnAny(
            "Modules.Carriers.Domain",
            "Modules.Carriers.Infrastructure",
            "Modules.Carriers.Features")
        .GetResult();
    
    Assert.True(result.IsSuccessful);
}
```

**Rules:**
1. ✅ Modules can reference `*.PublicApi` of other modules
2. ❌ Modules cannot reference `Domain`, `Infrastructure`, or `Features` of other modules
3. ✅ All modules can reference `Common.*` projects
4. ❌ Independent modules (Users, Carriers, Stocks) have zero cross-module dependencies
5. ✅ Shipments module can depend on Carriers.PublicApi and Stocks.PublicApi

## Module Independence

Each module is independently:
- **Developed** - Own domain, own features
- **Tested** - Unit and integration tests per module
- **Migrated** - Separate database schema and migrations
- **Deployed** - Same deployment, but logically isolated

This enables:
- Team autonomy (different teams can own different modules)
- Independent evolution (change one module without affecting others)
- Future extraction to microservices (if needed)

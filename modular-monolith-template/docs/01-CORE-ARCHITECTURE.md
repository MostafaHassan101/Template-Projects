# Core Architectural Principles and Design Patterns

## Overview
This is a **.NET 9 Modular Monolith** template implementing a shipping/logistics domain with four business modules: **Users**, **Shipments**, **Carriers**, and **Stocks**.

## Core Architectural Principles

### 1. **Modular Monolith Architecture**
- Single deployable unit with logical module boundaries
- Each module is independently developed but deployed together
- Modules communicate via well-defined contracts (PublicApi)
- Shared infrastructure and common libraries

### 2. **Domain-Driven Design (DDD)**
Each module follows DDD tactical patterns:
- **Entities**: Rich domain models with behavior (e.g., `Shipment` entity with state transitions)
- **Value Objects**: Immutable objects like `Address`
- **Aggregates**: `Shipment` acts as aggregate root managing `ShipmentItem` entities
- **Domain Logic**: Business rules encapsulated in domain entities (e.g., `Shipment.Process()`, `Shipment.Dispatch()`)

**Example from Shipment entity:**
```csharp
public Result<Success> Process()
{
    if (Status is not ShipmentStatus.Created)
    {
        return Error.Validation(ErrorCode, $"Can only update to Processing from Created status");
    }
    Status = ShipmentStatus.Processing;
    UpdatedAt = DateTime.UtcNow;
    return Result.Success;
}
```

### 3. **Vertical Slice Architecture**
- Features organized by use case, not technical layers
- Each feature contains: Endpoint → Handler → Domain → Persistence
- Located in `Features/[FeatureName]/` folders
- Example: `CreateShipment.Endpoint.cs`, `CreateShipment.Handler.cs`, `CreateShipment.Validator.cs`

### 4. **CQRS-Lite Pattern**
- Commands and Queries separated at the handler level
- No MediatR - custom lightweight handler pattern using `IHandler` marker interface
- Handlers registered via reflection and resolved through DI
- Example: `ICreateShipmentHandler`, `IGetShipmentByNumberHandler`

### 5. **Result Pattern**
- Custom `Result<TValue>` type for error handling
- No exceptions for business logic failures
- Type-safe error handling with `Error` types (Validation, NotFound, Conflict, etc.)

**Example:**
```csharp
public async Task<Result<ShipmentResponse>> HandleAsync(CreateShipmentRequest request, CancellationToken ct)
{
    if (shipmentExists)
        return ShipmentErrors.AlreadyExists(request.OrderId);
    
    var stockResponse = await stockApi.CheckStockAsync(stockRequest, ct);
    if (!stockResponse.IsSuccess)
        return stockResponse.Errors;
    
    return shipment.MapToResponse();
}
```

### 6. **Event-Driven Architecture (In-Process)**
- Custom event publisher/handler system (no external message bus)
- Events published via `IEventPublisher`
- Multiple handlers can subscribe to same event
- Handlers execute in parallel using `Task.WhenAll`

### 7. **Minimal API with Endpoint Pattern**
- No controllers - uses ASP.NET Core Minimal APIs
- Endpoints implement `IApiEndpoint` interface
- Auto-discovery and registration via reflection
- Clean separation of HTTP concerns from business logic

**Example:**
```csharp
public class CreateShipmentApiEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost(RouteConsts.BaseRoute, Handle);
    }
    
    private static async Task<IResult> Handle(
        [FromBody] CreateShipmentRequest request,
        IValidator<CreateShipmentRequest> validator,
        ICreateShipmentHandler handler,
        CancellationToken cancellationToken)
    {
        // Validation → Handler → Result mapping
    }
}
```

## Design Patterns Used

### 1. **Repository Pattern** (Implicit via DbContext)
- EF Core `DbContext` acts as Unit of Work
- `DbSet<T>` acts as repositories
- No explicit repository abstraction layer

### 2. **Decorator Pattern**
- Used for cross-cutting concerns like tracing
- Example: `TracedCarrierModuleApi` wraps `CarrierModuleApi`

### 3. **Factory Pattern**
- `IPolicyFactory` for authorization policies
- Static factory methods on entities (e.g., `Shipment.Create()`)

### 4. **Strategy Pattern**
- Different module middleware configurators (`IModuleMiddlewareConfigurator`)
- Each module can define custom middleware

### 5. **Dependency Injection**
- Constructor injection throughout
- Service registration via extension methods
- Scoped lifetime for handlers and DbContexts

### 6. **Marker Interface Pattern**
- `IHandler`, `IEventHandler`, `IApiEndpoint` for auto-discovery
- Enables convention-based registration

## Technology Stack

- **.NET 9** - Latest framework
- **ASP.NET Core** - Web framework with Minimal APIs
- **Entity Framework Core 9** - ORM with PostgreSQL
- **FluentValidation** - Request validation
- **Serilog** - Structured logging
- **OpenTelemetry** - Distributed tracing
- **JWT Authentication** - Security
- **Swagger/OpenAPI** - API documentation
- **xUnit** - Testing framework
- **NetArchTest** - Architecture testing
- **Testcontainers** - Integration testing
- **Bogus** - Test data generation

## Code Quality & Standards

### Static Analysis
- **Meziantou.Analyzer** - Best practices
- **SonarAnalyzer** - Code quality
- **Roslynator** - Code style
- **TreatWarningsAsErrors** - Strict compilation

### Configuration
- **Central Package Management** - `Directory.Packages.props`
- **EditorConfig** - Code style enforcement
- **Nullable Reference Types** - Enabled globally
- **Implicit Usings** - Enabled for cleaner code

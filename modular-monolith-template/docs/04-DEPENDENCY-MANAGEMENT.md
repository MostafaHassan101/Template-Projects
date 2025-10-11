# Dependency Management Strategy

## Central Package Management (CPM)

The solution uses **NuGet Central Package Management** for consistent versioning across all projects.

### Directory.Packages.props
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="9.0.7" />
    <PackageVersion Include="FluentValidation" Version="12.0.0" />
    <PackageVersion Include="Serilog.AspNetCore" Version="9.0.0" />
    <!-- All package versions defined centrally -->
  </ItemGroup>
</Project>
```

### Project Files (No Versions)
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
  <PackageReference Include="FluentValidation" />
</ItemGroup>
```

**Benefits:**
- Single source of truth for package versions
- Prevents version conflicts
- Easy to update all projects at once
- Enforces consistency

## Directory.Build.props

Global MSBuild properties applied to all projects:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AnalysisLevel>latest</AnalysisLevel>
    <AnalysisMode>All</AnalysisMode>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <CodeAnalysisTreatWarningsAsErrors>true</CodeAnalysisTreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Analyzers included in all projects -->
    <PackageReference Include="Meziantou.Analyzer" />
    <PackageReference Include="SonarAnalyzer.CSharp" />
    <PackageReference Include="Roslynator.Analyzers" />
  </ItemGroup>
</Project>
```

**Enforces:**
- Nullable reference types
- Strict warnings as errors
- Code analysis on build
- Consistent code style

## Dependency Injection Strategy

### 1. **Module Registration Pattern**

Each module provides extension methods for self-registration:

```csharp
// Shipments Module
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
```

**Host Registration:**
```csharp
// Program.cs
builder.Services
    .AddUsersModule(builder.Configuration)
    .AddShipmentsModule(builder.Configuration)
    .AddCarriersModule(builder.Configuration)
    .AddStocksModule(builder.Configuration);
```

### 2. **Auto-Discovery via Reflection**

#### Handler Registration
```csharp
public static IServiceCollection RegisterHandlersFromAssemblyContaining(
    this IServiceCollection services, 
    Type marker)
{
    var assembly = marker.Assembly;
    
    // Register command handlers (IHandler implementations)
    RegisterCommandHandlers(services, assembly);
    
    // Register event handlers (IEventHandler implementations)
    RegisterEventHandlers(services, assembly);
    
    return services;
}

private static void RegisterCommandHandlers(IServiceCollection services, Assembly assembly)
{
    var handlerTypes = assembly.GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: false }
            && t.IsAssignableTo(typeof(IHandler))
            && !t.IsAssignableTo(typeof(IEventHandler)))
        .ToList();
    
    foreach (var implementationType in handlerTypes)
    {
        var interfaceType = implementationType.GetInterfaces()
            .FirstOrDefault(i => i != typeof(IHandler) && i.IsAssignableTo(typeof(IHandler)));
        
        if (interfaceType is not null)
        {
            services.AddScoped(interfaceType, implementationType);
        }
    }
}
```

#### Endpoint Registration
```csharp
public static IServiceCollection RegisterApiEndpointsFromAssemblyContaining(
    this IServiceCollection services, 
    Type marker)
{
    var assembly = marker.Assembly;
    
    var endpointTypes = assembly.GetTypes()
        .Where(t => t.IsAssignableTo(typeof(IApiEndpoint)) 
            && t is { IsClass: true, IsAbstract: false, IsInterface: false });
    
    var serviceDescriptors = endpointTypes
        .Select(type => ServiceDescriptor.Transient(typeof(IApiEndpoint), type))
        .ToArray();
    
    services.TryAddEnumerable(serviceDescriptors);
    return services;
}
```

**Benefits:**
- No manual registration needed
- Convention-based
- Reduces boilerplate
- Hard to forget to register

### 3. **Service Lifetimes**

| Service Type | Lifetime | Reason |
|-------------|----------|--------|
| `DbContext` | Scoped | Per-request, manages database connection |
| `IHandler` | Scoped | May depend on DbContext |
| `IEventHandler` | Scoped | May depend on DbContext |
| `IEventPublisher` | Scoped | Resolves scoped handlers |
| `IApiEndpoint` | Transient | Stateless, lightweight |
| `IValidator` | Scoped | FluentValidation default |
| `IModuleApi` | Scoped | May depend on handlers/DbContext |
| `ILogger` | Singleton | Injected by framework |
| `IConfiguration` | Singleton | Application configuration |

## Project Dependencies

### Dependency Hierarchy

```
┌─────────────────────────────────────────────────────────────┐
│                     ModularMonolith.Host                    │
│  (Composition root - references all module Features)        │
└───────────────────────────┬─────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  Shipments   │    │   Carriers   │    │    Stocks    │
│  .Features   │    │  .Features   │    │  .Features   │
└──────┬───────┘    └──────┬───────┘    └──────┬───────┘
       │                   │                   │
       ├───────────────────┼───────────────────┤
       │                   │                   │
       ▼                   ▼                   ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  Shipments   │    │   Carriers   │    │    Stocks    │
│.Infrastructure│   │.Infrastructure│   │.Infrastructure│
└──────┬───────┘    └──────┬───────┘    └──────┬───────┘
       │                   │                   │
       ▼                   ▼                   ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  Shipments   │    │   Carriers   │    │    Stocks    │
│   .Domain    │    │   .Domain    │    │   .Domain    │
└──────────────┘    └──────────────┘    └──────────────┘
       │                   │                   │
       │                   │                   │
       │            ┌──────┴───────┐           │
       │            │   Carriers   │           │
       │            │  .PublicApi  │           │
       │            └──────┬───────┘           │
       │                   │                   │
       │            ┌──────┴───────┐           │
       │            │    Stocks    │           │
       │            │  .PublicApi  │           │
       │            └──────┬───────┘           │
       │                   │                   │
       └───────────────────┼───────────────────┘
                           │
                           ▼
                  ┌─────────────────┐
                  │ Common.Domain   │
                  │ Common.Application│
                  │ Common.Infrastructure│
                  │ Common.API      │
                  └─────────────────┘
```

### Module Dependency Rules

#### ✅ Allowed Dependencies

**Domain Projects:**
- ✅ `Common.Domain` only
- ❌ No other dependencies

**Infrastructure Projects:**
- ✅ Own `Domain` project
- ✅ `Common.Infrastructure`
- ✅ `Common.Domain`
- ✅ EF Core, Npgsql

**Features Projects:**
- ✅ Own `Domain` project
- ✅ Own `Infrastructure` project
- ✅ Other modules' `PublicApi` projects
- ✅ `Common.Application`
- ✅ `Common.API`
- ✅ `Common.Domain`
- ✅ FluentValidation, Bogus

**PublicApi Projects:**
- ✅ `Common.Domain` only (for Result types)
- ❌ No other dependencies

### Cross-Module Dependencies

**Shipments Module Dependencies:**
```csharp
// Shipments.Features.csproj
<ItemGroup>
  <ProjectReference Include="..\..\Carriers\Modules.Carriers.PublicApi\..." />
  <ProjectReference Include="..\..\Stocks\Modules.Stocks.PublicApi\..." />
  <ProjectReference Include="..\Modules.Shipments.Infrastructure\..." />
</ItemGroup>
```

**Carriers Module (No cross-module dependencies):**
```csharp
// Carriers.Features.csproj
<ItemGroup>
  <ProjectReference Include="..\Modules.Carriers.Domain\..." />
  <ProjectReference Include="..\Modules.Carriers.Infrastructure\..." />
  <ProjectReference Include="..\Modules.Carriers.PublicApi\..." />
  <ProjectReference Include="..\..\Common\Modules.Common.Application\..." />
</ItemGroup>
```

## Shared Infrastructure

### Common Projects

#### 1. **Common.Domain**
- `IEvent`, `IEventHandler`, `IEventPublisher`
- `IHandler` marker interface
- `Result<T>`, `Error`, `Success` types
- `IAuditableEntity`

**No external dependencies** - pure abstractions

#### 2. **Common.Application**
- `EventPublisher` implementation
- Handler registration extensions
- Shared application logic

**Dependencies:** `Common.Domain`, DI abstractions

#### 3. **Common.Infrastructure**
- Database migration infrastructure
- `IModuleDatabaseMigrator`
- `AuditableInterceptor` (EF Core interceptor)
- JWT authentication setup
- OpenTelemetry configuration
- Authorization policies

**Dependencies:** EF Core, OpenTelemetry, JWT, Npgsql

#### 4. **Common.API**
- `IApiEndpoint` abstraction
- Endpoint registration extensions
- `GlobalExceptionHandler`
- Result → HTTP mapping extensions
- Swagger configuration

**Dependencies:** ASP.NET Core, Swashbuckle

## Database Per Module

Each module has its own:
- **DbContext** (e.g., `ShipmentsDbContext`, `CarriersDbContext`)
- **Database Schema** (e.g., `shipments`, `carriers`, `stocks`)
- **Migration History** (separate `__EFMigrationsHistory` per schema)

```csharp
// Shipments Infrastructure
public static IServiceCollection AddShipmentsInfrastructure(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    var postgresConnectionString = configuration.GetConnectionString("Postgres");
    
    services.AddDbContext<ShipmentsDbContext>(x => x
        .UseNpgsql(postgresConnectionString, npgsqlOptions => 
            npgsqlOptions.MigrationsHistoryTable(
                DbConsts.MigrationHistoryTableName, 
                DbConsts.ShipmentsSchemaName)) // "shipments" schema
        .UseSnakeCaseNamingConvention()
    );
    
    services.AddScoped<IModuleDatabaseMigrator, ShipmentsDatabaseMigrator>();
    
    return services;
}
```

**Migration Execution:**
```csharp
// Program.cs
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.MigrateModuleDatabasesAsync(); // Migrates all modules
}

// Extension method
public static async Task MigrateModuleDatabasesAsync(this IServiceScope scope)
{
    var migrators = scope.ServiceProvider
        .GetRequiredService<IEnumerable<IModuleDatabaseMigrator>>();
    
    foreach (var migrator in migrators)
    {
        await migrator.MigrateAsync(scope);
    }
}
```

## Decorator Pattern for Cross-Cutting Concerns

### Tracing Decorator Example

```csharp
// Registration
services.AddScoped<CarrierModuleApi>();
services.AddScoped<ICarrierModuleApi>(provider =>
{
    var actualImplementation = provider.GetRequiredService<CarrierModuleApi>();
    return new TracedCarrierModuleApi(actualImplementation);
});

// Decorator implementation
internal sealed class TracedCarrierModuleApi : ICarrierModuleApi
{
    private readonly CarrierModuleApi _inner;
    
    public async Task<Result<Success>> CreateShipmentAsync(...)
    {
        using var activity = CarriersActivitySource.Instance
            .StartActivity("CarrierModuleApi.CreateShipment");
        
        activity?.SetTag("orderId", request.OrderId);
        
        return await _inner.CreateShipmentAsync(request, cancellationToken);
    }
}
```

**Benefits:**
- Separation of concerns
- Non-invasive
- Easy to add/remove
- Testable (can test with/without decorator)

## Testing Strategy

### Unit Tests
- Mock `IModuleApi` interfaces
- Mock `IEventPublisher`
- Test handlers in isolation

### Integration Tests
- Use Testcontainers for PostgreSQL
- Test full module stack
- Test cross-module communication

### Architecture Tests
- NetArchTest validates dependency rules
- Ensures modules don't violate boundaries
- Runs in CI/CD pipeline

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

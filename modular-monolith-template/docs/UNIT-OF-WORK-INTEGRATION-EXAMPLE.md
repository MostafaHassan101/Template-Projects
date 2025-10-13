# Unit of Work Pattern - Integration Example

## 📋 Step-by-Step Integration Guide

This guide shows how to integrate the Unit of Work pattern into the existing Shipments module.

---

## Step 1: Register Unit of Work

Update `Modules.Shipments.Infrastructure/DependencyInjection.cs`:

```csharp
using Modules.Common.Infrastructure.UnitOfWork;
using Modules.Shipments.Infrastructure.Repositories;

public static class DependencyInjection
{
    public static IServiceCollection AddShipmentsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<ShipmentsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("ShipmentsDb"),
                b => b.MigrationsAssembly(typeof(ShipmentsDbContext).Assembly.FullName)));

        // Register Unit of Work for ShipmentsDbContext
        services.AddUnitOfWork<ShipmentsDbContext>();

        // Register Repositories
        services.AddScoped<ShipmentRepository>();

        // Register Outbox
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}
```

---

## Step 2: Update Handler to Use Unit of Work

### Before (Direct DbContext):

```csharp
internal sealed class CreateShipmentHandler(
    ShipmentsDbContext context,
    IStockModuleApi stockApi,
    IEventPublisher eventPublisher,
    ILogger<CreateShipmentHandler> logger)
    : ICreateShipmentHandler
{
    public async Task<Result<ShipmentResponse>> HandleAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        // Check if shipment already exists
        var shipmentExists = await context.Shipments
            .AnyAsync(x => x.OrderId == request.OrderId, cancellationToken);
        
        if (shipmentExists)
        {
            return ShipmentErrors.AlreadyExists(request.OrderId);
        }

        // Create shipment
        var shipment = Shipment.Create(/* ... */);
        
        await context.Shipments.AddAsync(shipment, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        
        // Publish event
        await eventPublisher.PublishAsync(
            new ShipmentCreatedEvent(shipment), 
            cancellationToken);
        
        return ShipmentResponse.FromShipment(shipment);
    }
}
```

---

### After (With Unit of Work):

```csharp
using Modules.Common.Domain.UnitOfWork;
using Modules.Shipments.Infrastructure.Repositories;

internal sealed class CreateShipmentHandler(
    IUnitOfWork unitOfWork,
    ShipmentRepository shipmentRepository,
    IEventPublisher eventPublisher,
    ILogger<CreateShipmentHandler> logger)
    : ICreateShipmentHandler
{
    public async Task<Result<ShipmentResponse>> HandleAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        // Check if shipment already exists using repository
        var shipmentExists = await shipmentRepository
            .ExistsByOrderIdAsync(request.OrderId, cancellationToken);
        
        if (shipmentExists)
        {
            return ShipmentErrors.AlreadyExists(request.OrderId);
        }

        // Create shipment
        var shipment = Shipment.Create(
            GenerateShipmentId(),
            request.OrderId,
            request.Address,
            request.Carrier,
            request.ReceiverEmail,
            request.Items.Select(x => new ShipmentItem 
            { 
                Product = x.Product, 
                Quantity = x.Quantity 
            }).ToList());

        // Add through repository
        await shipmentRepository.AddAsync(shipment, cancellationToken);
        
        // Save changes through Unit of Work
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Shipment {ShipmentId} created for order {OrderId}", 
            shipment.Id, shipment.OrderId);

        // Publish event
        await eventPublisher.PublishAsync(
            new ShipmentCreatedEvent(shipment), 
            cancellationToken);
        
        return ShipmentResponse.FromShipment(shipment);
    }

    private static string GenerateShipmentId() => $"SHP{Guid.NewGuid():N}";
}
```

---

## Step 3: Complex Transaction Example

Here's an example with explicit transaction management:

```csharp
internal sealed class ProcessShipmentHandler(
    IUnitOfWork unitOfWork,
    ShipmentRepository shipmentRepository,
    IStockModuleApi stockApi,
    ICarrierModuleApi carrierApi,
    IEventPublisher eventPublisher,
    ILogger<ProcessShipmentHandler> logger)
{
    public async Task<Result> HandleAsync(
        ProcessShipmentRequest request,
        CancellationToken cancellationToken)
    {
        // Begin explicit transaction for multi-step operation
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Step 1: Get and validate shipment
            var shipment = await shipmentRepository.GetByIdAsync(
                request.ShipmentId, 
                cancellationToken);
            
            if (shipment == null)
            {
                return Error.NotFound("Shipment.NotFound", "Shipment not found");
            }

            // Step 2: Reserve stock
            var stockRequest = new CheckStockRequest(
                shipment.Items.Select(x => new ProductStock(x.Product, x.Quantity)).ToList());
            
            var stockResult = await stockApi.CheckStockAsync(stockRequest, cancellationToken);
            if (stockResult.IsError)
            {
                await transaction.RollbackAsync(cancellationToken);
                return stockResult.Errors;
            }

            // Step 3: Create carrier shipment
            var carrierRequest = new CreateCarrierShipmentRequest(
                shipment.OrderId,
                new Address(shipment.Address.Street, shipment.Address.City, shipment.Address.Zip),
                shipment.Carrier,
                shipment.ReceiverEmail,
                shipment.Items.Select(x => new CarrierShipmentItem(x.Product, x.Quantity)).ToList());
            
            var carrierResult = await carrierApi.CreateShipmentAsync(carrierRequest, cancellationToken);
            if (carrierResult.IsError)
            {
                await transaction.RollbackAsync(cancellationToken);
                return carrierResult.Errors;
            }

            // Step 4: Update shipment status
            shipment.MarkAsProcessed();
            shipmentRepository.Update(shipment);
            
            // Save all changes
            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Commit transaction
            await transaction.CommitAsync(cancellationToken);
            
            logger.LogInformation("Shipment {ShipmentId} processed successfully", shipment.Id);

            // Publish event (after transaction commits)
            await eventPublisher.PublishAsync(
                new ShipmentProcessedEvent(shipment), 
                cancellationToken);
            
            return Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing shipment {ShipmentId}", request.ShipmentId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

---

## Step 4: Query Examples Using Repository

```csharp
public class GetShipmentsQueryHandler(
    ShipmentRepository shipmentRepository,
    ILogger<GetShipmentsQueryHandler> logger)
{
    public async Task<Result<List<ShipmentResponse>>> HandleAsync(
        GetShipmentsQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Shipment> shipments;

        // Use repository methods based on query type
        if (!string.IsNullOrEmpty(query.Carrier))
        {
            shipments = await shipmentRepository.GetByCarrierAsync(
                query.Carrier, 
                cancellationToken);
        }
        else if (!string.IsNullOrEmpty(query.Status))
        {
            shipments = await shipmentRepository.GetByStatusAsync(
                query.Status, 
                cancellationToken);
        }
        else if (query.StartDate.HasValue && query.EndDate.HasValue)
        {
            shipments = await shipmentRepository.GetByDateRangeAsync(
                query.StartDate.Value,
                query.EndDate.Value,
                cancellationToken);
        }
        else
        {
            shipments = await shipmentRepository.GetAllAsync(cancellationToken);
        }

        var responses = shipments
            .Select(ShipmentResponse.FromShipment)
            .ToList();

        return responses;
    }
}
```

---

## Step 5: Testing with Unit of Work

```csharp
public class CreateShipmentHandlerTests
{
    [Fact]
    public async Task CreateShipment_ShouldSaveChanges_WhenValid()
    {
        // Arrange
        var unitOfWorkMock = Substitute.For<IUnitOfWork>();
        var repositoryMock = Substitute.For<ShipmentRepository>();
        var eventPublisherMock = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<CreateShipmentHandler>>();

        var handler = new CreateShipmentHandler(
            unitOfWorkMock,
            repositoryMock,
            eventPublisherMock,
            logger);

        var request = new CreateShipmentRequest(/* ... */);

        repositoryMock.ExistsByOrderIdAsync(request.OrderId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await repositoryMock.Received(1).AddAsync(
            Arg.Any<Shipment>(), 
            Arg.Any<CancellationToken>());
        await unitOfWorkMock.Received(1).SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateShipment_ShouldNotSave_WhenShipmentExists()
    {
        // Arrange
        var unitOfWorkMock = Substitute.For<IUnitOfWork>();
        var repositoryMock = Substitute.For<ShipmentRepository>();
        var eventPublisherMock = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<CreateShipmentHandler>>();

        var handler = new CreateShipmentHandler(
            unitOfWorkMock,
            repositoryMock,
            eventPublisherMock,
            logger);

        var request = new CreateShipmentRequest(/* ... */);

        repositoryMock.ExistsByOrderIdAsync(request.OrderId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        await repositoryMock.DidNotReceive().AddAsync(
            Arg.Any<Shipment>(), 
            Arg.Any<CancellationToken>());
        await unitOfWorkMock.DidNotReceive().SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }
}
```

---

## 🎯 Benefits Achieved

✅ **Loose Coupling**: Handlers depend on `IUnitOfWork`, not `DbContext`  
✅ **Testability**: Easy to mock `IUnitOfWork` and repositories  
✅ **Transaction Control**: Explicit transactions for complex operations  
✅ **Consistency**: All data access goes through repositories  
✅ **Maintainability**: Clear separation between business logic and data access  

---

## 📊 Comparison

| Aspect | Direct DbContext | With Unit of Work |
|--------|------------------|-------------------|
| **Coupling** | Tight (depends on EF Core) | Loose (depends on interface) |
| **Testing** | Requires in-memory DB | Easy mocking |
| **Transactions** | Manual `BeginTransaction()` | Abstracted `BeginTransactionAsync()` |
| **Consistency** | Mixed patterns | Uniform pattern |
| **Flexibility** | Limited | High (can swap implementations) |

---

## ✅ Migration Checklist

- [x] Create `IUnitOfWork` and `IUnitOfWorkTransaction` interfaces
- [x] Implement `UnitOfWork<TContext>` and `UnitOfWorkTransaction`
- [x] Create base `Repository<TEntity, TContext>` class
- [x] Create `ShipmentRepository` inheriting from base
- [x] Register Unit of Work in DI container
- [x] Update handlers to use `IUnitOfWork` instead of `DbContext`
- [x] Update tests to mock `IUnitOfWork`
- [x] Document usage patterns

---

## 🚀 Next Steps

1. Apply the same pattern to other modules (Stocks, Carriers)
2. Create repositories for other entities
3. Add integration tests
4. Consider adding specification pattern for complex queries
5. Add repository caching if needed

---

Your data access layer is now clean, testable, and maintainable! 🎉

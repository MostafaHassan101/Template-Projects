# Unit of Work Pattern - Implementation Guide

## 🎯 Overview

The Unit of Work pattern maintains a list of objects affected by a business transaction and coordinates the writing out of changes. It provides a centralized way to manage database transactions across multiple repositories.

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Handler/Service                        │
│  (Business Logic)                                           │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    Unit of Work                             │
│  • Manages DbContext                                        │
│  • Coordinates transactions                                 │
│  • Tracks changes                                           │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                   Repositories                              │
│  • ShipmentRepository                                       │
│  • OrderRepository                                          │
│  • StockRepository                                          │
└─────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                     Database                                │
└─────────────────────────────────────────────────────────────┘
```

---

## 📦 Components

### 1. **IUnitOfWork** - Interface

Defines the contract for Unit of Work:

```csharp
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
```

### 2. **IUnitOfWorkTransaction** - Transaction Interface

Represents a database transaction:

```csharp
public interface IUnitOfWorkTransaction : IDisposable, IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
```

### 3. **UnitOfWork<TContext>** - Implementation

EF Core implementation wrapping DbContext:

```csharp
public class UnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    private readonly TContext _context;
    
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new UnitOfWorkTransaction(transaction, _logger);
    }
}
```

### 4. **Repository<TEntity, TContext>** - Base Repository

Generic repository with common CRUD operations:

```csharp
public class Repository<TEntity, TContext> where TEntity : class where TContext : DbContext
{
    protected readonly TContext Context;
    protected readonly DbSet<TEntity> DbSet;
    
    public virtual async Task<TEntity?> GetByIdAsync<TKey>(TKey id, CancellationToken cancellationToken = default)
    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    public virtual void Update(TEntity entity)
    public virtual void Remove(TEntity entity)
    // ... more methods
}
```

---

## 🚀 Usage Examples

### Example 1: Simple Save Changes

```csharp
public class CreateShipmentHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ShipmentRepository _shipmentRepository;
    
    public CreateShipmentHandler(IUnitOfWork unitOfWork, ShipmentRepository shipmentRepository)
    {
        _unitOfWork = unitOfWork;
        _shipmentRepository = shipmentRepository;
    }
    
    public async Task<Result<ShipmentResponse>> HandleAsync(
        CreateShipmentRequest request, 
        CancellationToken cancellationToken)
    {
        // Create shipment
        var shipment = Shipment.Create(
            request.ShipmentId,
            request.OrderId,
            request.Address,
            request.Carrier,
            request.ReceiverEmail,
            request.Items);
        
        // Add to repository
        await _shipmentRepository.AddAsync(shipment, cancellationToken);
        
        // Save changes through Unit of Work
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return ShipmentResponse.FromShipment(shipment);
    }
}
```

---

### Example 2: Explicit Transaction

```csharp
public class ProcessOrderHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly OrderRepository _orderRepository;
    private readonly StockRepository _stockRepository;
    
    public async Task<Result> HandleAsync(ProcessOrderRequest request, CancellationToken cancellationToken)
    {
        // Begin explicit transaction
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // 1. Update order status
            var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                return Error.NotFound("Order.NotFound", "Order not found");
            }
            
            order.MarkAsProcessing();
            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // 2. Decrease stock
            foreach (var item in request.Items)
            {
                var stock = await _stockRepository.GetByProductAsync(item.Product, cancellationToken);
                if (stock == null || stock.AvailableQuantity < item.Quantity)
                {
                    // Rollback on insufficient stock
                    await transaction.RollbackAsync(cancellationToken);
                    return Error.Validation("Stock.Insufficient", "Insufficient stock");
                }
                
                stock.DecreaseQuantity(item.Quantity);
                _stockRepository.Update(stock);
            }
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // 3. Commit transaction
            await transaction.CommitAsync(cancellationToken);
            
            return Result.Success;
        }
        catch (Exception ex)
        {
            // Rollback on any error
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

---

### Example 3: Multiple Repositories

```csharp
public class CreateShipmentWithInventoryHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ShipmentRepository _shipmentRepository;
    private readonly InventoryRepository _inventoryRepository;
    private readonly AuditRepository _auditRepository;
    
    public async Task<Result> HandleAsync(CreateShipmentRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // 1. Create shipment
            var shipment = Shipment.Create(/* ... */);
            await _shipmentRepository.AddAsync(shipment, cancellationToken);
            
            // 2. Update inventory
            foreach (var item in request.Items)
            {
                var inventory = await _inventoryRepository.GetByProductAsync(item.Product, cancellationToken);
                inventory.Reserve(item.Quantity);
                _inventoryRepository.Update(inventory);
            }
            
            // 3. Create audit log
            var auditLog = new AuditLog
            {
                Action = "ShipmentCreated",
                EntityId = shipment.Id,
                Timestamp = DateTime.UtcNow
            };
            await _auditRepository.AddAsync(auditLog, cancellationToken);
            
            // Save all changes in one transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            return Result.Success;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

---

## 🔧 Registration

### Step 1: Register Unit of Work

In your module's DependencyInjection.cs:

```csharp
using Modules.Common.Infrastructure.UnitOfWork;

public static IServiceCollection AddShipmentsModule(this IServiceCollection services)
{
    // Register DbContext
    services.AddDbContext<ShipmentsDbContext>(options => 
        options.UseSqlServer(connectionString));
    
    // Register Unit of Work
    services.AddUnitOfWork<ShipmentsDbContext>();
    
    // Register repositories
    services.AddScoped<ShipmentRepository>();
    services.AddScoped<OrderRepository>();
    
    return services;
}
```

---

### Step 2: Create Custom Repository

```csharp
using Modules.Common.Infrastructure.Repositories;

public class ShipmentRepository : Repository<Shipment, ShipmentsDbContext>
{
    public ShipmentRepository(ShipmentsDbContext context) : base(context)
    {
    }
    
    // Add custom methods
    public async Task<Shipment?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken)
    {
        return await FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);
    }
    
    public async Task<IReadOnlyList<Shipment>> GetPendingShipmentsAsync(CancellationToken cancellationToken)
    {
        return await FindAsync(s => s.Status == ShipmentStatus.Pending, cancellationToken);
    }
}
```

---

## 🎯 Benefits

| Benefit | Description |
|---------|-------------|
| **Transaction Management** | Centralized control over database transactions |
| **Consistency** | Ensures all changes succeed or fail together |
| **Testability** | Easy to mock for unit testing |
| **Separation of Concerns** | Business logic separated from data access |
| **Flexibility** | Can switch between auto-commit and explicit transactions |

---

## 🔍 Patterns Comparison

### Without Unit of Work (Direct DbContext)

```csharp
public class CreateShipmentHandler
{
    private readonly ShipmentsDbContext _context;
    
    public async Task HandleAsync(CreateShipmentRequest request, CancellationToken cancellationToken)
    {
        var shipment = Shipment.Create(/* ... */);
        await _context.Shipments.AddAsync(shipment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken); // Direct DbContext call
    }
}
```

**Issues:**
- ❌ Tight coupling to DbContext
- ❌ Hard to test
- ❌ No transaction abstraction
- ❌ Difficult to coordinate multiple repositories

---

### With Unit of Work

```csharp
public class CreateShipmentHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ShipmentRepository _repository;
    
    public async Task HandleAsync(CreateShipmentRequest request, CancellationToken cancellationToken)
    {
        var shipment = Shipment.Create(/* ... */);
        await _repository.AddAsync(shipment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken); // Abstracted
    }
}
```

**Benefits:**
- ✅ Loose coupling
- ✅ Easy to test (mock IUnitOfWork)
- ✅ Transaction abstraction
- ✅ Coordinates multiple repositories

---

## 🎓 Best Practices

### 1. **One Unit of Work Per Request**

```csharp
// Register as Scoped (one per HTTP request)
services.AddScoped<IUnitOfWork, UnitOfWork<ShipmentsDbContext>>();
```

### 2. **Explicit Transactions for Complex Operations**

```csharp
// Use explicit transactions when coordinating multiple operations
await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
try
{
    // Multiple operations
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
}
catch
{
    await transaction.RollbackAsync(cancellationToken);
    throw;
}
```

### 3. **Auto-Commit for Simple Operations**

```csharp
// For single repository operations, just call SaveChangesAsync
await _repository.AddAsync(entity, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);
```

### 4. **Repository Inheritance**

```csharp
// Inherit from base Repository for common operations
public class ShipmentRepository : Repository<Shipment, ShipmentsDbContext>
{
    public ShipmentRepository(ShipmentsDbContext context) : base(context) { }
    
    // Add custom methods only
    public async Task<Shipment?> GetByOrderIdAsync(string orderId, CancellationToken ct)
    {
        return await FirstOrDefaultAsync(s => s.OrderId == orderId, ct);
    }
}
```

### 5. **Don't Mix Patterns**

```csharp
// ❌ BAD: Mixing DbContext and Unit of Work
public class Handler
{
    private readonly ShipmentsDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task HandleAsync()
    {
        await _context.SaveChangesAsync(); // Don't do this
        await _unitOfWork.SaveChangesAsync(); // Use only one
    }
}

// ✅ GOOD: Use only Unit of Work
public class Handler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ShipmentRepository _repository;
    
    public async Task HandleAsync()
    {
        await _repository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(); // Consistent
    }
}
```

---

## 🧪 Testing

### Mock Unit of Work

```csharp
[Fact]
public async Task CreateShipment_ShouldSaveChanges()
{
    // Arrange
    var unitOfWorkMock = Substitute.For<IUnitOfWork>();
    var repositoryMock = Substitute.For<ShipmentRepository>();
    var handler = new CreateShipmentHandler(unitOfWorkMock, repositoryMock);
    
    // Act
    await handler.HandleAsync(request, CancellationToken.None);
    
    // Assert
    await unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
}
```

---

## 📊 Performance Considerations

| Aspect | Impact | Recommendation |
|--------|--------|----------------|
| **Transaction Overhead** | Minimal | Use explicit transactions only when needed |
| **Change Tracking** | Medium | Use `AsNoTracking()` for read-only queries |
| **SaveChanges Frequency** | High | Batch changes, call SaveChanges once |
| **Memory Usage** | Low | Unit of Work is lightweight wrapper |

---

## 🔐 Transaction Isolation Levels

```csharp
// Custom isolation level
await using var transaction = await _context.Database.BeginTransactionAsync(
    IsolationLevel.ReadCommitted, 
    cancellationToken);
```

**Levels:**
- `ReadUncommitted` - Fastest, least safe
- `ReadCommitted` - Default, balanced
- `RepeatableRead` - Prevents dirty reads
- `Serializable` - Slowest, most safe

---

## ✅ Summary

The Unit of Work pattern provides:

✅ **Centralized transaction management**  
✅ **Consistent data access layer**  
✅ **Easy testing with mocks**  
✅ **Separation of concerns**  
✅ **Flexible transaction control**  
✅ **Coordinates multiple repositories**  

Use it to build maintainable, testable, and reliable data access code! 🚀

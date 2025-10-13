# Base Entity Infrastructure Usage Guide

## Overview

The base entity infrastructure provides a robust foundation for domain entities with built-in support for:
- **Identity Management**: Unique identifiers with type safety
- **Audit Tracking**: Automatic tracking of creation and modification timestamps and users
- **Soft Delete**: Logical deletion without physical removal from the database
- **Equality Comparison**: Proper entity equality based on identity

---

## Entity Base Classes

### 1. BaseEntity<TId>

Generic base entity with a typed identifier.

```csharp
public abstract class BaseEntity<TId> where TId : notnull
{
    public TId Id { get; protected set; }
}
```

**Features:**
- Type-safe identity
- Proper equality comparison based on ID
- Overridden `Equals()`, `GetHashCode()`, and equality operators

**Usage:**
```csharp
public class Product : BaseEntity<int>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }
}
```

### 2. BaseEntity

Convenience base class using `Guid` as the identifier.

```csharp
public abstract class BaseEntity : BaseEntity<Guid>
{
}
```

**Usage:**
```csharp
public class Order : BaseEntity
{
    public string OrderNumber { get; private set; }
    public decimal TotalAmount { get; private set; }
}
```

### 3. AuditableEntity<TId>

Base entity with audit tracking capabilities.

```csharp
public abstract class AuditableEntity<TId> : BaseEntity<TId>, IAuditableEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
```

**Features:**
- Automatic timestamp tracking via interceptor
- User tracking (CreatedBy, UpdatedBy)
- Inherits all BaseEntity features

**Usage:**
```csharp
public class Customer : AuditableEntity<Guid>
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    
    public static Customer Create(string name, string email)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email
            // CreatedAtUtc will be set automatically by the interceptor
        };
    }
}
```

### 4. AuditableEntity

Convenience class using `Guid` identifier.

```csharp
public abstract class AuditableEntity : AuditableEntity<Guid>
{
}
```

### 5. AuditableSoftDeleteEntity<TId>

Base entity with both audit tracking and soft delete support.

```csharp
public abstract class AuditableSoftDeleteEntity<TId> : AuditableEntity<TId>, ISoftDelete
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
}
```

**Features:**
- All auditable entity features
- Soft delete support
- Automatic query filtering (excludes deleted entities)
- Physical deletion prevention via interceptor

**Usage:**
```csharp
public class BlogPost : AuditableSoftDeleteEntity<Guid>
{
    public string Title { get; private set; }
    public string Content { get; private set; }
    
    public void Delete()
    {
        // Just mark as deleted, interceptor handles the rest
        IsDeleted = true;
    }
}
```

### 6. AuditableSoftDeleteEntity

Convenience class using `Guid` identifier.

```csharp
public abstract class AuditableSoftDeleteEntity : AuditableSoftDeleteEntity<Guid>
{
}
```

---

## Interfaces

### IAuditableEntity

```csharp
public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
}
```

### ISoftDelete

```csharp
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
    string? DeletedBy { get; set; }
}
```

---

## Infrastructure Setup

### 1. Register the Interceptor

Add the `AuditableEntityInterceptor` to your DbContext:

```csharp
public class YourDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new AuditableEntityInterceptor());
    }
}
```

Or register it in your DI container:

```csharp
services.AddDbContext<YourDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(connectionString)
           .AddInterceptors(new AuditableEntityInterceptor());
});
```

### 2. Apply Soft Delete Query Filters

In your DbContext's `OnModelCreating` method:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Apply soft delete query filters globally
    modelBuilder.ApplySoftDeleteQueryFilters();
}
```

---

## Usage Examples

### Example 1: Simple Entity with Audit

```csharp
public class Category : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    
    private Category() { }
    
    public static Category Create(string name, string description)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description
            // CreatedAtUtc is set automatically
        };
    }
    
    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
        // UpdatedAtUtc is set automatically
    }
}
```

### Example 2: Entity with Soft Delete

```csharp
public class Comment : AuditableSoftDeleteEntity
{
    public string Content { get; private set; } = null!;
    public Guid PostId { get; private set; }
    public Guid AuthorId { get; private set; }
    
    private Comment() { }
    
    public static Comment Create(string content, Guid postId, Guid authorId)
    {
        return new Comment
        {
            Id = Guid.NewGuid(),
            Content = content,
            PostId = postId,
            AuthorId = authorId
        };
    }
    
    public void Delete()
    {
        // Soft delete - entity remains in database
        IsDeleted = true;
        // DeletedAtUtc is set automatically by interceptor
    }
}
```

### Example 3: Querying with Soft Delete

```csharp
// Normal query - excludes soft deleted entities automatically
var activeComments = await context.Comments
    .Where(c => c.PostId == postId)
    .ToListAsync();

// Include soft deleted entities
var allComments = await context.Comments
    .IncludeSoftDeleted()
    .Where(c => c.PostId == postId)
    .ToListAsync();

// Only soft deleted entities
var deletedComments = await context.Comments
    .OnlySoftDeleted()
    .Where(c => c.PostId == postId)
    .ToListAsync();
```

### Example 4: Custom ID Type

```csharp
public class Invoice : AuditableEntity<long>
{
    public string InvoiceNumber { get; private set; } = null!;
    public decimal Amount { get; private set; }
    
    public static Invoice Create(long id, string invoiceNumber, decimal amount)
    {
        return new Invoice
        {
            Id = id,
            InvoiceNumber = invoiceNumber,
            Amount = amount
        };
    }
}
```

---

## Best Practices

### 1. Use Private Setters
Protect entity state by using private setters and factory methods:

```csharp
public class Product : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    
    private Product() { } // EF Core constructor
    
    public static Product Create(string name, decimal price)
    {
        // Validation logic here
        return new Product { Id = Guid.NewGuid(), Name = name, Price = price };
    }
}
```

### 2. Implement Business Logic in Domain
Keep business rules in the entity:

```csharp
public class Order : AuditableSoftDeleteEntity
{
    public OrderStatus Status { get; private set; }
    
    public Result Cancel()
    {
        if (Status == OrderStatus.Shipped)
            return Error.Validation("Order.CannotCancel", "Cannot cancel shipped orders");
            
        Status = OrderStatus.Cancelled;
        IsDeleted = true; // Soft delete
        return Result.Success();
    }
}
```

### 3. Use Appropriate Base Class
- **BaseEntity**: Simple entities without audit needs
- **AuditableEntity**: Entities requiring timestamp tracking
- **AuditableSoftDeleteEntity**: Entities that should never be physically deleted

### 4. Configure User Context
Update the interceptor to capture the current user:

```csharp
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    
    public AuditableEntityInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }
    
    private void UpdateAuditableEntities(DbContext? context)
    {
        var userId = _currentUserService.UserId;
        
        // Set CreatedBy, UpdatedBy, DeletedBy with userId
    }
}
```

---

## Migration Guide

### Migrating Existing Entities

**Before:**
```csharp
public class Shipment
{
    public Guid Id { get; private init; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
}
```

**After:**
```csharp
public class Shipment : AuditableEntity
{
    // Remove Id, CreatedAt, UpdatedAt - inherited from base
    // Rename CreatedAt -> CreatedAtUtc if needed
}
```

### Database Migration

```bash
# Generate migration for new audit fields
dotnet ef migrations add AddAuditFields

# Update database
dotnet ef database update
```

---

## Testing

### Unit Testing Entities

```csharp
[Fact]
public void Create_ShouldSetId()
{
    var product = Product.Create("Test", 10.99m);
    
    Assert.NotEqual(Guid.Empty, product.Id);
}

[Fact]
public void Equals_ShouldCompareById()
{
    var id = Guid.NewGuid();
    var product1 = new Product { Id = id };
    var product2 = new Product { Id = id };
    
    Assert.Equal(product1, product2);
}
```

### Integration Testing with Interceptor

```csharp
[Fact]
public async Task SaveChanges_ShouldSetCreatedAtUtc()
{
    var product = Product.Create("Test", 10.99m);
    
    await context.Products.AddAsync(product);
    await context.SaveChangesAsync();
    
    Assert.NotEqual(default, product.CreatedAtUtc);
}
```

---

## Summary

The base entity infrastructure provides:
✅ Type-safe identity management
✅ Automatic audit tracking
✅ Soft delete with query filtering
✅ Proper equality comparison
✅ Reduced boilerplate code
✅ Consistent entity behavior across modules

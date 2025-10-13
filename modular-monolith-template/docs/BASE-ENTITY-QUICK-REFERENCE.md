# Base Entity Quick Reference

## 📦 Available Base Classes

| Base Class | ID Type | Audit | Soft Delete | Use Case |
|------------|---------|-------|-------------|----------|
| `BaseEntity<TId>` | Custom | ❌ | ❌ | Simple entities with custom ID |
| `BaseEntity` | Guid | ❌ | ❌ | Simple entities with Guid ID |
| `AuditableEntity<TId>` | Custom | ✅ | ❌ | Audited entities with custom ID |
| `AuditableEntity` | Guid | ✅ | ❌ | Audited entities with Guid ID |
| `AuditableSoftDeleteEntity<TId>` | Custom | ✅ | ✅ | Full-featured with custom ID |
| `AuditableSoftDeleteEntity` | Guid | ✅ | ✅ | Full-featured with Guid ID |

---

## 🚀 Quick Start

### 1. Create an Entity

```csharp
public class Product : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    
    private Product() { }
    
    public static Product Create(string name, decimal price)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price
        };
    }
}
```

### 2. Register Interceptor

```csharp
services.AddDbContext<YourDbContext>(options =>
{
    options.UseSqlServer(connectionString)
           .AddInterceptors(new AuditableEntityInterceptor());
});
```

### 3. Apply Soft Delete Filters

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplySoftDeleteQueryFilters();
}
```

---

## 📝 Common Patterns

### Entity with Validation

```csharp
public class Email : AuditableEntity
{
    public string Address { get; private set; } = null!;
    
    public static Result<Email> Create(string address)
    {
        if (!IsValidEmail(address))
            return Error.Validation("Email.Invalid", "Invalid email format");
            
        return new Email { Id = Guid.NewGuid(), Address = address };
    }
}
```

### Entity with Soft Delete

```csharp
public class Comment : AuditableSoftDeleteEntity
{
    public string Content { get; private set; } = null!;
    
    public Result Delete()
    {
        if (IsDeleted)
            return Error.Validation("Comment.AlreadyDeleted", "Comment is already deleted");
            
        IsDeleted = true;
        return Result.Success();
    }
}
```

### Entity with Custom ID

```csharp
public class Invoice : AuditableEntity<long>
{
    public string Number { get; private set; } = null!;
    
    public static Invoice Create(long id, string number)
    {
        return new Invoice { Id = id, Number = number };
    }
}
```

---

## 🔍 Query Extensions

```csharp
// Normal query - excludes soft deleted
var active = await context.Comments.ToListAsync();

// Include soft deleted
var all = await context.Comments.IncludeSoftDeleted().ToListAsync();

// Only soft deleted
var deleted = await context.Comments.OnlySoftDeleted().ToListAsync();
```

---

## 🏗️ Properties Reference

### BaseEntity<TId>
```csharp
TId Id { get; protected set; }
```

### IAuditableEntity
```csharp
DateTime CreatedAtUtc { get; set; }      // Set automatically
DateTime? UpdatedAtUtc { get; set; }     // Set automatically
string? CreatedBy { get; set; }          // TODO: Implement user context
string? UpdatedBy { get; set; }          // TODO: Implement user context
```

### ISoftDelete
```csharp
bool IsDeleted { get; set; }             // Set manually or via Delete()
DateTime? DeletedAtUtc { get; set; }     // Set automatically
string? DeletedBy { get; set; }          // TODO: Implement user context
```

---

## ⚡ Best Practices

✅ **DO:**
- Use private setters for encapsulation
- Create factory methods for entity creation
- Implement business logic in the entity
- Use appropriate base class for your needs

❌ **DON'T:**
- Expose public setters
- Create entities with `new` keyword (use factory methods)
- Set audit properties manually
- Hard delete entities that implement ISoftDelete

---

## 🔧 Troubleshooting

### Audit fields not being set?
- Ensure interceptor is registered
- Check DbContext is using the interceptor
- Verify entity inherits from IAuditableEntity

### Soft deleted entities still appearing?
- Ensure `ApplySoftDeleteQueryFilters()` is called
- Check entity implements ISoftDelete
- Verify you're not using `IgnoreQueryFilters()`

### Equality comparison not working?
- Ensure entities inherit from BaseEntity
- Check IDs are properly set
- Verify you're comparing entities of the same type

---

## 📚 Full Documentation

See `docs/BASE-ENTITY-USAGE.md` for complete documentation and examples.

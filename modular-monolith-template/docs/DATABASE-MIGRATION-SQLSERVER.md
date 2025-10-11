# Database Provider Migration: PostgreSQL → SQL Server

## Summary of Changes

I've successfully migrated the database provider from **PostgreSQL** to **SQL Server**. Here's what was changed:

## 1. Package Updates

### Removed Packages:
- ❌ `Npgsql.EntityFrameworkCore.PostgreSQL`
- ❌ `Npgsql.OpenTelemetry`
- ❌ `EFCore.NamingConventions` (SQL Server uses standard naming)

### Added Packages:
- ✅ `Microsoft.EntityFrameworkCore.SqlServer` (Version 9.0.7)
- ✅ `OpenTelemetry.Instrumentation.SqlClient` (Version 1.9.0)

## 2. Code Changes

### All Module DependencyInjection Files

**Changed in:**
- `Shipments/Modules.Shipments.Infrastructure/DependencyInjection.cs`
- `Carriers/Modules.Carriers.Infrastructure/DependencyInjection.cs`
- `Stocks/Modules.Stocks.Infrastructure/DependencyInjection.cs`
- `Users/Modules.Users.Infrastructure/DependencyInjection.cs`

**Before:**
```csharp
var postgresConnectionString = configuration.GetConnectionString("Postgres");

services.AddDbContext<ShipmentsDbContext>(x => x
    .UseNpgsql(postgresConnectionString, npgsqlOptions => 
        npgsqlOptions.MigrationsHistoryTable(...))
    .UseSnakeCaseNamingConvention()
);
```

**After:**
```csharp
var sqlServerConnectionString = configuration.GetConnectionString("SqlServer");

services.AddDbContext<ShipmentsDbContext>(x => x
    .UseSqlServer(sqlServerConnectionString, sqlServerOptions => 
        sqlServerOptions.MigrationsHistoryTable(...))
);
```

### Common Infrastructure

**File:** `Common/Modules.Common.Infrastructure/DependencyInjection.cs`

**Before:**
```csharp
using Npgsql;

tracing
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddNpgsql()
    .AddSource(activityModuleNames);
```

**After:**
```csharp
tracing
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddSqlClientInstrumentation()
    .AddSource(activityModuleNames);
```

## 3. Configuration Changes Required

### Update appsettings.json

You need to update your `appsettings.json` file:

**Before:**
```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=ModularMonolith;Username=postgres;Password=yourpassword"
  }
}
```

**After:**
```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=ModularMonolith;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### Alternative (Windows Authentication):
```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=ModularMonolith;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

## 4. Database Schema Support

✅ **Good News:** SQL Server supports schemas just like PostgreSQL!

The modular structure is preserved:
- `users` schema
- `shipments` schema
- `carriers` schema
- `stocks` schema

Each module maintains its own schema and migration history table.

## 5. Next Steps - Regenerate Migrations

⚠️ **Important:** You need to regenerate all EF Core migrations for SQL Server.

### Delete Existing Migrations

```powershell
# Navigate to each module and delete Migrations folder
Remove-Item -Recurse -Force .\Shipments\Modules.Shipments.Infrastructure\Database\Migrations\*
Remove-Item -Recurse -Force .\Carriers\Modules.Carriers.Infrastructure\Database\Migrations\*
Remove-Item -Recurse -Force .\Stocks\Modules.Stocks.Infrastructure\Database\Migrations\*
Remove-Item -Recurse -Force .\Users\Modules.Users.Infrastructure\Database\Migrations\*
```

### Create New Migrations

```powershell
# Shipments Module
dotnet ef migrations add InitialCreate `
  --project .\Shipments\Modules.Shipments.Infrastructure `
  --startup-project .\ModularMonolith.Host `
  --context ShipmentsDbContext `
  --output-dir Database/Migrations

# Carriers Module
dotnet ef migrations add InitialCreate `
  --project .\Carriers\Modules.Carriers.Infrastructure `
  --startup-project .\ModularMonolith.Host `
  --context CarriersDbContext `
  --output-dir Database/Migrations

# Stocks Module
dotnet ef migrations add InitialCreate `
  --project .\Stocks\Modules.Stocks.Infrastructure `
  --startup-project .\ModularMonolith.Host `
  --context StocksDbContext `
  --output-dir Database/Migrations

# Users Module
dotnet ef migrations add InitialCreate `
  --project .\Users\Modules.Users.Infrastructure `
  --startup-project .\ModularMonolith.Host `
  --context UsersDbContext `
  --output-dir Database/Migrations
```

### Apply Migrations

The application will automatically apply migrations on startup in Development mode (as per `Program.cs`):

```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.MigrateModuleDatabasesAsync();
}
```

Or manually:
```powershell
dotnet ef database update --project .\Shipments\Modules.Shipments.Infrastructure --startup-project .\ModularMonolith.Host
dotnet ef database update --project .\Carriers\Modules.Carriers.Infrastructure --startup-project .\ModularMonolith.Host
dotnet ef database update --project .\Stocks\Modules.Stocks.Infrastructure --startup-project .\ModularMonolith.Host
dotnet ef database update --project .\Users\Modules.Users.Infrastructure --startup-project .\ModularMonolith.Host
```

## 6. Docker Compose Changes (Optional)

If you're using Docker, update `docker-compose.yml`:

**Before:**
```yaml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_PASSWORD: yourpassword
    ports:
      - "5432:5432"
```

**After:**
```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "YourStrong@Password"
    ports:
      - "1433:1433"
```

## 7. Key Differences: PostgreSQL vs SQL Server

| Feature | PostgreSQL | SQL Server |
|---------|-----------|------------|
| **Naming Convention** | snake_case (via EFCore.NamingConventions) | PascalCase (default) |
| **Schema Support** | ✅ Yes | ✅ Yes |
| **Case Sensitivity** | Case-sensitive by default | Case-insensitive by default |
| **Data Types** | `text`, `jsonb`, `uuid` | `nvarchar(max)`, `json`, `uniqueidentifier` |
| **Sequences** | Native support | Identity columns |

## 8. Testing Changes

### Integration Tests

If you have integration tests using Testcontainers, you'll need to update them:

**Before:**
```csharp
private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
    .WithDatabase("testdb")
    .Build();
```

**After:**
```csharp
private readonly MsSqlContainer _container = new MsSqlBuilder()
    .WithPassword("YourStrong@Password")
    .Build();
```

## 9. Restore and Build

```powershell
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run application
dotnet run --project .\ModularMonolith.Host
```

## 10. Verification Checklist

- [ ] Updated `appsettings.json` with SQL Server connection string
- [ ] SQL Server instance is running (LocalDB, Express, or full version)
- [ ] Deleted old PostgreSQL migrations
- [ ] Generated new SQL Server migrations for all modules
- [ ] Application builds without errors
- [ ] Application starts and creates database schemas
- [ ] All modules can read/write to their respective schemas
- [ ] OpenTelemetry tracing works with SQL Server
- [ ] Integration tests updated (if applicable)

## Troubleshooting

### Error: "A network-related or instance-specific error occurred"
- Ensure SQL Server is running
- Check connection string
- Verify firewall settings
- Enable TCP/IP in SQL Server Configuration Manager

### Error: "Login failed for user"
- Check username/password
- Verify SQL Server authentication mode (Mixed Mode)
- Ensure user has proper permissions

### Error: "Cannot create database"
- Ensure user has `dbcreator` role
- Or create database manually first

### Schema Not Created
- SQL Server creates schemas automatically when migrations run
- Ensure migrations include schema creation
- Check `HasDefaultSchema()` in DbContext

## Benefits of SQL Server

✅ **Better Windows Integration**
✅ **Integrated Security (Windows Auth)**
✅ **SQL Server Management Studio (SSMS)**
✅ **Azure SQL Database compatibility**
✅ **Enterprise features (if using full version)**
✅ **Better .NET tooling integration**

## Rollback (If Needed)

To revert back to PostgreSQL:
1. Restore the original files from git
2. Run: `git checkout HEAD -- src/`
3. Restore packages: `dotnet restore`

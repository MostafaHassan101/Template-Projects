# Quick Migration Steps - PostgreSQL to SQL Server

## ✅ What's Been Fixed

All code has been updated to use SQL Server instead of PostgreSQL:

1. ✅ All `DependencyInjection.cs` files updated
2. ✅ All `.csproj` files updated
3. ✅ `Directory.Packages.props` updated
4. ✅ Integration tests updated (Testcontainers)
5. ✅ OpenTelemetry instrumentation updated

## 🚀 Steps to Complete Migration

### Step 1: Restore Packages
```powershell
cd src
dotnet restore
```

### Step 2: Delete Old Migrations
```powershell
# Run the provided script
.\delete-old-migrations.ps1

# OR manually delete these folders:
# - Shipments\Modules.Shipments.Infrastructure\Database\Migrations\*
# - Carriers\Modules.Carriers.Infrastructure\Database\Migrations\*
# - Stocks\Modules.Stocks.Infrastructure\Database\Migrations\*
# - Users\Modules.Users.Infrastructure\Database\Migrations\*
```

### Step 3: Update appsettings.json
```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=ModularMonolith;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

**Alternative (SQL Server Authentication):**
```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=ModularMonolith;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### Step 4: Regenerate Migrations
```powershell
# Run the provided script
.\regenerate-migrations.ps1

# This will create new SQL Server migrations for all 4 modules
```

### Step 5: Build and Run
```powershell
dotnet build
dotnet run --project .\ModularMonolith.Host
```

The application will automatically create the database and apply migrations on startup (in Development mode).

## 📋 Verification Checklist

- [ ] `dotnet restore` completed successfully
- [ ] Old PostgreSQL migrations deleted
- [ ] `appsettings.json` updated with SQL Server connection string
- [ ] SQL Server is running (LocalDB, Express, or full version)
- [ ] New migrations generated for all 4 modules
- [ ] `dotnet build` succeeds without errors
- [ ] Application starts without errors
- [ ] Database and schemas created automatically
- [ ] Can perform CRUD operations

## 🔍 What Changed

### Packages
- ❌ Removed: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Npgsql.OpenTelemetry`, `EFCore.NamingConventions`
- ✅ Added: `Microsoft.EntityFrameworkCore.SqlServer`, `OpenTelemetry.Instrumentation.SqlClient`

### Code
- Changed `UseNpgsql()` → `UseSqlServer()`
- Removed `.UseSnakeCaseNamingConvention()` (SQL Server uses PascalCase by default)
- Changed connection string key: `"Postgres"` → `"SqlServer"`
- Updated integration tests to use `Testcontainers.MsSql`

### Database
- Still uses **separate schemas** per module (shipments, carriers, stocks, users)
- Migration history tables remain separate per module
- Same modular architecture preserved

## ⚠️ Common Issues

### Issue: "Cannot connect to SQL Server"
**Solution:** Ensure SQL Server is running. For LocalDB:
```powershell
sqllocaldb start mssqllocaldb
```

### Issue: "Login failed"
**Solution:** Use Windows Authentication (Integrated Security=True) or create SQL Server login

### Issue: "Migrations not found"
**Solution:** Make sure you deleted old migrations and regenerated new ones

### Issue: "Build errors about Npgsql"
**Solution:** Run `dotnet clean` then `dotnet restore` then `dotnet build`

## 📚 Additional Resources

- Full migration guide: `docs/DATABASE-MIGRATION-SQLSERVER.md`
- Architecture documentation: `docs/01-CORE-ARCHITECTURE.md`

## 🔄 Rollback (If Needed)

To revert to PostgreSQL:
```powershell
git checkout HEAD -- src/
dotnet restore
```

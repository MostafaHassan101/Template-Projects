# PowerShell script to regenerate migrations for SQL Server
# Run this from the src directory AFTER deleting old migrations and running dotnet restore

Write-Host "Regenerating migrations for SQL Server..." -ForegroundColor Yellow
Write-Host ""

# Shipments Module
Write-Host "Creating Shipments migration..." -ForegroundColor Cyan
dotnet ef migrations add InitialCreate `
  --project .\Shipments\Modules.Shipments.Infrastructure `
  --startup-project .\ModularMonolith.Host `
  --context ShipmentsDbContext `
  --output-dir Database/Migrations

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Shipments migration created" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to create Shipments migration" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Carriers Module
Write-Host "Creating Carriers migration..." -ForegroundColor Cyan
dotnet ef migrations add InitialCreate `
  --project .\Carriers\Modules.Carriers.Infrastructure `
  --startup-project .\ModularMonolith.Host `
  --context CarriersDbContext `
  --output-dir Database/Migrations

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Carriers migration created" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to create Carriers migration" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Stocks Module
Write-Host "Creating Stocks migration..." -ForegroundColor Cyan
dotnet ef migrations add InitialCreate `
  --project .\Stocks\Modules.Stocks.Infrastructure `
  --startup-project .\ModularMonolith.Host `
  --context StocksDbContext `
  --output-dir Database/Migrations

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Stocks migration created" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to create Stocks migration" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Users Module
Write-Host "Creating Users migration..." -ForegroundColor Cyan
dotnet ef migrations add InitialCreate `
  --project .\Users\Modules.Users.Infrastructure `
  --startup-project .\ModularMonolith.Host `
  --context UsersDbContext `
  --output-dir Database/Migrations

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Users migration created" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to create Users migration" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "All migrations regenerated successfully!" -ForegroundColor Cyan
Write-Host "You can now run the application and it will automatically apply migrations." -ForegroundColor Yellow

# PowerShell script to delete old PostgreSQL migrations
# Run this from the src directory

Write-Host "Deleting old PostgreSQL migrations..." -ForegroundColor Yellow

# Delete Shipments migrations
$shipmentsPath = ".\Shipments\Modules.Shipments.Infrastructure\Database\Migrations"
if (Test-Path $shipmentsPath) {
    Remove-Item -Path "$shipmentsPath\*" -Recurse -Force
    Write-Host "✓ Deleted Shipments migrations" -ForegroundColor Green
}

# Delete Carriers migrations
$carriersPath = ".\Carriers\Modules.Carriers.Infrastructure\Database\Migrations"
if (Test-Path $carriersPath) {
    Remove-Item -Path "$carriersPath\*" -Recurse -Force
    Write-Host "✓ Deleted Carriers migrations" -ForegroundColor Green
}

# Delete Stocks migrations
$stocksPath = ".\Stocks\Modules.Stocks.Infrastructure\Database\Migrations"
if (Test-Path $stocksPath) {
    Remove-Item -Path "$stocksPath\*" -Recurse -Force
    Write-Host "✓ Deleted Stocks migrations" -ForegroundColor Green
}

# Delete Users migrations
$usersPath = ".\Users\Modules.Users.Infrastructure\Database\Migrations"
if (Test-Path $usersPath) {
    Remove-Item -Path "$usersPath\*" -Recurse -Force
    Write-Host "✓ Deleted Users migrations" -ForegroundColor Green
}

Write-Host "`nAll old migrations deleted successfully!" -ForegroundColor Cyan
Write-Host "Now run 'dotnet restore' and then regenerate migrations for SQL Server." -ForegroundColor Yellow

using ModularMonolith.Host.Seeding;
using Modules.Common.API.Extensions;
using Modules.Common.Infrastructure.Database;
using Modules.Common.Infrastructure.Outbox;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebHostDependencies();

builder.AddCoreHostLogging();

builder.Services.AddCoreWebApiInfrastructure();

builder.Services.AddCoreInfrastructure(builder.Configuration,
[
    ShipmentsModuleRegistration.ActivityModuleName,
    CarriersModuleRegistration.ActivityModuleName,
    StocksModuleRegistration.ActivityModuleName
]);

builder.Services
    .AddUsersModule(builder.Configuration)
    .AddShipmentsModule(builder.Configuration)
    .AddCarriersModule(builder.Configuration)
    .AddStocksModule(builder.Configuration);

// Register Outbox Processor as a background service
builder.Services.AddHostedService<OutboxProcessor>();

// Seed entities in DEVELOPMENT mode
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<SeedService>();
}

var app = builder.Build();

// Run migrations in DEVELOPMENT mode
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.MigrateModuleDatabasesAsync();

    var userSeedService = scope.ServiceProvider.GetRequiredService<UserSeedService>();
    await userSeedService.SeedUsersAsync();

    var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
    await seedService.SeedDataAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseModuleMiddlewares();

app.MapApiEndpoints();

await app.RunAsync();

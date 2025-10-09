using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Modules.Common.API.Abstractions;
using Modules.Common.Application.Extensions;
using Modules.Stocks.Features.InternalApi;
using Modules.Stocks.Features.InternalApi.Decorators;
using Modules.Stocks.Features.Tracing;
using Modules.Stocks.PublicApi;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class StocksModuleRegistration
{
    public static string ActivityModuleName => StocksActivitySource.Instance.Name;

    public static IServiceCollection AddStocksModule(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddStocksModuleApi()
            .AddStocksInfrastructure(configuration);
    }
    
    private static IServiceCollection AddStocksModuleApi(this IServiceCollection services)
    {
        services.AddScoped<StockModuleApi>();

        services.AddScoped<IStockModuleApi>(provider =>
        {
            var actualImplementation = provider.GetRequiredService<StockModuleApi>();
            return new TracedStockModuleApi(actualImplementation);
        });
        
        services.RegisterApiEndpointsFromAssemblyContaining(typeof(StocksModuleRegistration));
        
        services.RegisterHandlersFromAssemblyContaining(typeof(StocksModuleRegistration));
        
        services.AddValidatorsFromAssembly(typeof(StocksModuleRegistration).Assembly);

        return services;
    }
}

public class StocksMiddlewareConfigurator : IModuleMiddlewareConfigurator
{
    public IApplicationBuilder Configure(IApplicationBuilder app)
    {
        return app.UseMiddleware<StocksTracingMiddleware>();
    }
}

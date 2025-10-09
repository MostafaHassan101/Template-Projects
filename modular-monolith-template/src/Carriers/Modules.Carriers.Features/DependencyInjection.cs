using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Modules.Carriers.Features.InternalApi;
using Modules.Carriers.Features.InternalApi.Decorators;
using Modules.Carriers.Features.Tracing;
using Modules.Carriers.PublicApi;
using Modules.Common.API.Abstractions;
using Modules.Common.Application.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class CarriersModuleRegistration
{
    public static string ActivityModuleName => CarriersActivitySource.Instance.Name;

    public static IServiceCollection AddCarriersModule(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddCarriersModuleApi()
            .AddCarriersInfrastructure(configuration);
    }
    
    private static IServiceCollection AddCarriersModuleApi(this IServiceCollection services)
    {
        services.AddScoped<CarrierModuleApi>();

        services.AddScoped<ICarrierModuleApi>(provider =>
        {
            var actualImplementation = provider.GetRequiredService<CarrierModuleApi>();
            return new TracedCarrierModuleApi(actualImplementation);
        });

        services.RegisterApiEndpointsFromAssemblyContaining(typeof(CarriersModuleRegistration));
        
        services.RegisterHandlersFromAssemblyContaining(typeof(CarriersModuleRegistration));
        
        services.AddValidatorsFromAssembly(typeof(CarriersModuleRegistration).Assembly);

        return services;
    }
}

public class CarriersMiddlewareConfigurator : IModuleMiddlewareConfigurator
{
    public IApplicationBuilder Configure(IApplicationBuilder app)
    {
        return app.UseMiddleware<CarriersTracingMiddleware>();
    }
}

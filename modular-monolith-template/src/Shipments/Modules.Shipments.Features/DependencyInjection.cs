using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Modules.Common.API.Abstractions;
using Modules.Common.Application;
using Modules.Common.Application.Extensions;
using Modules.Common.Domain.Events;
using Modules.Shipments.Features.Tracing;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ShipmentsModuleRegistration
{
    public static string ActivityModuleName => ShipmentsTracingConsts.ActivityModuleName;
    
    public static IServiceCollection AddShipmentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddShipmentsModuleApi()
            .AddShipmentsInfrastructure(configuration);
    }
    
    private static IServiceCollection AddShipmentsModuleApi(this IServiceCollection services)
    {
        services.AddScoped<IEventPublisher, EventPublisher>();

        services.RegisterApiEndpointsFromAssemblyContaining(typeof(ShipmentsModuleRegistration));
        
        services.RegisterHandlersFromAssemblyContaining(typeof(ShipmentsModuleRegistration));

        services.AddValidatorsFromAssembly(typeof(ShipmentsModuleRegistration).Assembly);
        
        return services;
    }
}

public class ShipmentsMiddlewareConfigurator : IModuleMiddlewareConfigurator
{
    public IApplicationBuilder Configure(IApplicationBuilder app)
    {
        return app.UseMiddleware<ShipmentsTracingMiddleware>();
    }
}

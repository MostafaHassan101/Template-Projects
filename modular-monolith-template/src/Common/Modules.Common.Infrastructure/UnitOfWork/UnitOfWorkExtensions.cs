using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modules.Common.Domain.UnitOfWork;

namespace Modules.Common.Infrastructure.UnitOfWork;

/// <summary>
/// Extension methods for registering Unit of Work in DI container.
/// </summary>
public static class UnitOfWorkExtensions
{
    /// <summary>
    /// Registers Unit of Work for a specific DbContext.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();
        return services;
    }
}

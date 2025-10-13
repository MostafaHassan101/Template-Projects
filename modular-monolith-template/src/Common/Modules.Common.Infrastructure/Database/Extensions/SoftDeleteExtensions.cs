using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Modules.Common.Domain.Entities;
using System.Linq.Expressions;
using System.Reflection;

namespace Modules.Common.Infrastructure.Database.Extensions;

/// <summary>
/// Extension methods for configuring soft delete behavior in Entity Framework Core.
/// </summary>
public static class SoftDeleteExtensions
{
    /// <summary>
    /// Applies global query filters for soft delete to all entities implementing ISoftDelete.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    public static void ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
    {
        var softDeleteEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(entityType => typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType));

        foreach (var entityType in softDeleteEntityTypes)
        {
			#pragma warning disable S3011 // Accessibility bypass is safe here

			var method = typeof(SoftDeleteExtensions)
                .GetMethod(nameof(ApplySoftDeleteQueryFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(null, [modelBuilder, entityType]);
			#pragma warning restore S3011

		}
	}

    /// <summary>
    /// Applies soft delete query filter to a specific entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <param name="entityType">The entity type metadata.</param>
    /// <remarks>
    /// This method uses reflection to access a private method, which is safe because it's accessing
    /// a method within the same class and the method is guaranteed to exist.
    /// </remarks>
    private static void ApplySoftDeleteQueryFilter<TEntity>(
        ModelBuilder modelBuilder,
        IMutableEntityType entityType) where TEntity : class, ISoftDelete
    {
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
        var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(false)), parameter);

        var existingFilter = entityType.GetQueryFilter();
        if (existingFilter != null)
        {
            // Combine with existing filter
            var combinedBody = Expression.AndAlso(
                existingFilter.Body,
                Expression.Invoke(filter, existingFilter.Parameters[0]));
            filter = Expression.Lambda(combinedBody, existingFilter.Parameters[0]);
        }

        modelBuilder.Entity<TEntity>().HasQueryFilter((Expression<Func<TEntity, bool>>)filter);
    }

    /// <summary>
    /// Includes soft deleted entities in the query.
    /// </summary>
    /// <param name="query">The query to modify.</param>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>A query that includes soft deleted entities.</returns>
    public static IQueryable<TEntity> IncludeSoftDeleted<TEntity>(this IQueryable<TEntity> query)
        where TEntity : class, ISoftDelete
    {
        return query.IgnoreQueryFilters();
    }

    /// <summary>
    /// Filters to only soft deleted entities.
    /// </summary>
    /// <param name="query">The query to modify.</param>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>A query that returns only soft deleted entities.</returns>
    public static IQueryable<TEntity> OnlySoftDeleted<TEntity>(this IQueryable<TEntity> query)
        where TEntity : class, ISoftDelete
    {
        return query.IgnoreQueryFilters().Where(e => e.IsDeleted);
    }
}

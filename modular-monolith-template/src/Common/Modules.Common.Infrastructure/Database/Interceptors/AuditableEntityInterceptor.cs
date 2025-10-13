using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Modules.Common.Domain;
using Modules.Common.Domain.Entities;

namespace Modules.Common.Infrastructure.Database.Interceptors;

/// <summary>
/// Interceptor that automatically sets audit fields and handles soft delete for entities.
/// </summary>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateAuditableEntities(DbContext? context)
    {
		if (context is null)
		{
			return;
		}

        var utcNow = DateTime.UtcNow;
        var entries = context.ChangeTracker.Entries<IAuditableEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = utcNow;
                    entry.Entity.UpdatedAtUtc = null;
                    // TODO: Set CreatedBy from current user context
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = utcNow;
                    // TODO: Set UpdatedBy from current user context
                    break;

                case EntityState.Deleted:
                    // Handle soft delete
                    if (entry.Entity is ISoftDelete softDeleteEntity)
                    {
                        entry.State = EntityState.Modified;
                        softDeleteEntity.IsDeleted = true;
                        softDeleteEntity.DeletedAtUtc = utcNow;
                        // TODO: Set DeletedBy from current user context
                    }
                    break;
            }
        }
    }
}

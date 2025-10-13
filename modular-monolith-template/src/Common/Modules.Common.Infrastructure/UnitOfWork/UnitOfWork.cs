using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.UnitOfWork;

namespace Modules.Common.Infrastructure.UnitOfWork;

/// <summary>
/// EF Core implementation of Unit of Work pattern.
/// Wraps DbContext to provide transaction management and change tracking.
/// </summary>
public class UnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    private readonly TContext _context;
    private readonly ILogger<UnitOfWork<TContext>> _logger;
    private bool _disposed;

    public UnitOfWork(TContext context, ILogger<UnitOfWork<TContext>> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Saved {Count} changes to database", result);
            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict occurred while saving changes");
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error occurred while saving changes");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while saving changes");
            throw;
        }
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Beginning new transaction");
        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new UnitOfWorkTransaction(transaction, _logger);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // DbContext is managed by DI container, don't dispose it here
            _disposed = true;
        }
    }
}

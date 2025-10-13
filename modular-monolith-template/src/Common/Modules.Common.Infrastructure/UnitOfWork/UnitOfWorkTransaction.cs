using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Modules.Common.Domain.UnitOfWork;

namespace Modules.Common.Infrastructure.UnitOfWork;

/// <summary>
/// EF Core implementation of database transaction for Unit of Work.
/// </summary>
internal sealed class UnitOfWorkTransaction : IUnitOfWorkTransaction
{
    private readonly IDbContextTransaction _transaction;
    private readonly ILogger _logger;
    private bool _disposed;

    public UnitOfWorkTransaction(IDbContextTransaction transaction, ILogger logger)
    {
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _transaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Transaction committed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing transaction");
            throw;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _transaction.RollbackAsync(cancellationToken);
            _logger.LogWarning("Transaction rolled back");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back transaction");
            throw;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction.Dispose();
            _disposed = true;
        }
    }

    private async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            await _transaction.DisposeAsync();
            _disposed = true;
        }
    }
}

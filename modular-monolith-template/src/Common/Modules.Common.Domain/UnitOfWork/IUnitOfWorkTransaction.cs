namespace Modules.Common.Domain.UnitOfWork;

/// <summary>
/// Represents a database transaction within a unit of work.
/// </summary>
public interface IUnitOfWorkTransaction : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Commits the transaction, persisting all changes to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction, discarding all changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

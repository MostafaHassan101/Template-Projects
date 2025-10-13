namespace Modules.Common.Domain.Entities;

/// <summary>
/// Base auditable entity class with audit tracking properties.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class AuditableEntity<TId> : BaseEntity<TId>, IAuditableEntity where TId : notnull
{
    /// <summary>
    /// Gets or sets the date and time when the entity was created (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was last updated (UTC).
    /// </summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created the entity.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last updated the entity.
    /// </summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Base auditable entity class with a Guid identifier.
/// </summary>
public abstract class AuditableEntity : AuditableEntity<Guid>;

/// <summary>
/// Base auditable entity class with soft delete support.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class AuditableSoftDeleteEntity<TId> : AuditableEntity<TId>, ISoftDelete where TId : notnull
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity is deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was deleted (UTC).
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who deleted the entity.
    /// </summary>
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Base auditable entity class with soft delete support and Guid identifier.
/// </summary>
public abstract class AuditableSoftDeleteEntity : AuditableSoftDeleteEntity<Guid>;

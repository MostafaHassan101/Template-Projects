namespace Modules.Common.Domain;

/// <summary>
/// Interface for entities that support audit tracking.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Gets or sets the date and time when the entity was created (UTC).
    /// </summary>
    DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was last updated (UTC).
    /// </summary>
    DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created the entity.
    /// </summary>
    string? CreatedBy { get; set; }

	/// <summary>
	/// Gets or sets the identifier of the user who last updated the entity.
	/// </summary>
	string? UpdatedBy { get; set; }
}

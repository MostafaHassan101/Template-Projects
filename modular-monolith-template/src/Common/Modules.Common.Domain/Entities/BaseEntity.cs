namespace Modules.Common.Domain.Entities;

/// <summary>
/// Base entity class with a unique identifier.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class BaseEntity<TId> where TId : notnull
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Determines whether the specified entity is equal to the current entity.
    /// </summary>
    public override bool Equals(object? obj)
    {
		if (obj is not BaseEntity<TId> other)
		{
			return false;
		}

        if (ReferenceEquals(this, other))
        {
			return true;
		}

        if (GetType() != other.GetType())
        {
			return false;
		}

        if (EqualityComparer<TId>.Default.Equals(Id, default) || 
            EqualityComparer<TId>.Default.Equals(other.Id, default))
        {
			return false;
		}

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <summary>
    /// Returns the hash code for this entity.
    /// </summary>
    public override int GetHashCode()
    {
		return StringComparer.OrdinalIgnoreCase.GetHashCode((GetType().ToString() + Id).ToLowerInvariant());
	}
	

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(BaseEntity<TId>? left, BaseEntity<TId>? right) =>
		!(left == right);

	/// <summary>
	/// Equality operator.
	/// </summary>
	#pragma warning disable S3875 // Type defines operator == but does not override Object.Equals
	public static bool operator ==(BaseEntity<TId>? left, BaseEntity<TId>? right)
	{
		if (left is null && right is null)
		{
			return true;
		}

		if (left is null || right is null)
		{
			return false;
		}

		return left.Equals(right);
	}
	#pragma warning restore CS0660


}

/// <summary>
/// Base entity class with a Guid identifier.
/// </summary>
public abstract class BaseEntity : BaseEntity<Guid>;

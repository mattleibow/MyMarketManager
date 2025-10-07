namespace MyMarketManager.Data;

/// <summary>
/// Interface for entities that support soft delete and audit tracking.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// When the entity was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the entity was last updated.
    /// </summary>
    DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// When the entity was soft deleted. Null if not deleted.
    /// </summary>
    DateTimeOffset? DeletedAt { get; set; }
}

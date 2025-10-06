namespace MyMarketManager.Data;

/// <summary>
/// Base class for all entities with audit tracking and soft delete support.
/// </summary>
public abstract class EntityBase : IAuditable
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// When the entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// When the entity was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
    
    /// <summary>
    /// When the entity was soft deleted. Null if not deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }
    
    /// <summary>
    /// Whether the entity is deleted (soft delete).
    /// </summary>
    public bool IsDeleted => DeletedAt.HasValue;
}

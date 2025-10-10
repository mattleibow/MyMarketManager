using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Entities;

/// <summary>
/// Tracks web scraping sessions for suppliers.
/// </summary>
public class ScraperSession : EntityBase
{
    /// <summary>
    /// The supplier this scraping session is for.
    /// </summary>
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    /// <summary>
    /// When this scraping session started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// When this scraping session completed (if successful).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// The status of this scraping session.
    /// </summary>
    public ProcessingStatus Status { get; set; }

    /// <summary>
    /// The staging batch created by this session, if any.
    /// </summary>
    public Guid? StagingBatchId { get; set; }
    public StagingBatch? StagingBatch { get; set; }

    /// <summary>
    /// Serialized cookie file JSON for this session.
    /// </summary>
    public string? CookieFileJson { get; set; }

    /// <summary>
    /// Error message if the session failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional notes or metadata about this session.
    /// </summary>
    public string? Notes { get; set; }
}

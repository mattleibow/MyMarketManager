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
    public ScraperSessionStatus Status { get; set; }

    /// <summary>
    /// The staging batch created by this session, if any.
    /// </summary>
    public Guid? StagingBatchId { get; set; }
    public StagingBatch? StagingBatch { get; set; }

    /// <summary>
    /// Timestamp of the last successful scrape before this one.
    /// Used to determine what data is new.
    /// </summary>
    public DateTimeOffset? LastSuccessfulScrape { get; set; }

    /// <summary>
    /// Number of pages scraped in this session.
    /// </summary>
    public int PagesScraped { get; set; }

    /// <summary>
    /// Number of orders found/scraped in this session.
    /// </summary>
    public int OrdersScraped { get; set; }

    /// <summary>
    /// Error message if the session failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional notes or metadata about this session.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Status of a scraper session.
/// </summary>
public enum ScraperSessionStatus
{
    /// <summary>
    /// Session is queued to start.
    /// </summary>
    Queued = 0,

    /// <summary>
    /// Session is currently running.
    /// </summary>
    Running = 1,

    /// <summary>
    /// Session completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Session failed with an error.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Session was cancelled.
    /// </summary>
    Cancelled = 4
}

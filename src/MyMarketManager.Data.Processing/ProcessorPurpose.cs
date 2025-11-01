namespace MyMarketManager.Data.Processing;

/// <summary>
/// Defines the purpose or category of a batch processor.
/// Used to filter and display processors in different parts of the UI.
/// </summary>
public enum ProcessorPurpose
{
    /// <summary>
    /// Processors for data ingestion (e.g., web scrapers, file uploads).
    /// Typically shown on ingestion/import pages.
    /// </summary>
    Ingestion = 0,

    /// <summary>
    /// Internal background processors (e.g., vectorization, cleanup).
    /// Not typically shown in user-facing UI.
    /// </summary>
    Internal = 1,

    /// <summary>
    /// Processors for data export or reporting.
    /// </summary>
    Export = 2
}

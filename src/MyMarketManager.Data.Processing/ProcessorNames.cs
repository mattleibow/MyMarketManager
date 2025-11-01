namespace MyMarketManager.Data.Processing;

/// <summary>
/// Well-known processor names used throughout the system.
/// Using constants ensures consistency and avoids runtime errors from typos.
/// </summary>
public static class ProcessorNames
{
    /// <summary>
    /// Shein web scraper processor name.
    /// </summary>
    public const string SheinWebScraper = "Shein";

    /// <summary>
    /// Image vectorization processor name.
    /// </summary>
    public const string ImageVectorization = "ImageVectorization";
}

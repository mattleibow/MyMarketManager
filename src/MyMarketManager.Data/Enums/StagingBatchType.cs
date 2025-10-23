namespace MyMarketManager.Data.Enums;

/// <summary>
/// The type of staging batch, indicating the source of the data.
/// </summary>
public enum StagingBatchType
{
    /// <summary>
    /// Data was scraped from a website.
    /// </summary>
    WebScrape = 0,

    /// <summary>
    /// Data was uploaded from a blob/file.
    /// </summary>
    BlobUpload = 1
}

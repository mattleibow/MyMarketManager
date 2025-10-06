namespace MyMarketManager.Data.Enums;

/// <summary>
/// Used to rate the quality of products and delivered items.
/// </summary>
public enum ProductQuality
{
    /// <summary>
    /// Superior condition, no defects
    /// </summary>
    Excellent,

    /// <summary>
    /// Minor imperfections, fully functional
    /// </summary>
    Good,

    /// <summary>
    /// Noticeable flaws but acceptable
    /// </summary>
    Fair,

    /// <summary>
    /// Significant defects, limited usability
    /// </summary>
    Poor,

    /// <summary>
    /// Severe damage, may not be sellable
    /// </summary>
    Terrible
}

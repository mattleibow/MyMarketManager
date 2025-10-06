namespace MyMarketManager.Data.Enums;

/// <summary>
/// Used to rate the quality of products and delivered items.
/// Values 1-5 can be used as "X out of 5" ratings, with 0 meaning no rating yet.
/// </summary>
public enum ProductQuality
{
    /// <summary>
    /// No rating yet
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Severe damage, may not be sellable (1 out of 5)
    /// </summary>
    Terrible = 1,

    /// <summary>
    /// Significant defects, limited usability (2 out of 5)
    /// </summary>
    Poor = 2,

    /// <summary>
    /// Noticeable flaws but acceptable (3 out of 5)
    /// </summary>
    Fair = 3,

    /// <summary>
    /// Minor imperfections, fully functional (4 out of 5)
    /// </summary>
    Good = 4,

    /// <summary>
    /// Superior condition, no defects (5 out of 5)
    /// </summary>
    Excellent = 5
}

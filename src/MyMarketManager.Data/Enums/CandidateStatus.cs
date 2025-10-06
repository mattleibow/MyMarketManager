namespace MyMarketManager.Data.Enums;

/// <summary>
/// Used to track the state of staging items during the validation and linking process.
/// </summary>
public enum CandidateStatus
{
    /// <summary>
    /// Awaiting review and linking
    /// </summary>
    Pending,

    /// <summary>
    /// Successfully matched to an existing entity
    /// </summary>
    Linked,

    /// <summary>
    /// Marked to be skipped during import
    /// </summary>
    Ignored
}

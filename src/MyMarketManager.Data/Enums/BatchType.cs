namespace MyMarketManager.Data.Enums;

/// <summary>
/// Represents the type of data in a staging batch.
/// </summary>
public enum BatchType
{
    /// <summary>
    /// Supplier purchase order data from various supplier exports
    /// </summary>
    SupplierData,

    /// <summary>
    /// Sales data from point-of-sale systems (e.g., Yoco API)
    /// </summary>
    SalesData
}

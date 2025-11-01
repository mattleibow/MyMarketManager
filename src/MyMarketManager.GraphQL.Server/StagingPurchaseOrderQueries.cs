using HotChocolate;
using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.GraphQL.Server;

/// <summary>
/// GraphQL queries for staging purchase orders
/// </summary>
[ExtendObjectType("Query")]
public class StagingPurchaseOrderQueries
{
    /// <summary>
    /// Get a staging purchase order by ID with all items
    /// </summary>
    public async Task<StagingPurchaseOrderDetailDto?> GetStagingPurchaseOrderById(
        Guid id,
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        var order = await context.StagingPurchaseOrders
            .Include(spo => spo.Items)
                .ThenInclude(i => i.Product)
            .Include(spo => spo.Items)
                .ThenInclude(i => i.Supplier)
            .Include(spo => spo.StagingBatch)
                .ThenInclude(sb => sb.Supplier)
            .FirstOrDefaultAsync(spo => spo.Id == id, cancellationToken);

        if (order == null)
        {
            return null;
        }

        return new StagingPurchaseOrderDetailDto(
            order.Id,
            order.SupplierReference,
            order.OrderDate,
            order.Status,
            order.IsImported,
            order.ErrorMessage,
            order.StagingBatch.Supplier?.Name,
            order.Items.Select(i => new StagingPurchaseOrderItemDto(
                i.Id,
                i.Name,
                i.Description,
                i.SupplierReference,
                i.SupplierProductUrl,
                i.Quantity,
                i.ListedUnitPrice,
                i.ActualUnitPrice,
                i.Status,
                i.IsImported,
                i.ProductId,
                i.Product != null ? new LinkedProductDto(
                    i.Product.Id,
                    i.Product.SKU,
                    i.Product.Name,
                    i.Product.Quality) : null,
                i.SupplierId,
                i.Supplier?.Name)).ToList());
    }

    /// <summary>
    /// Search products for matching suggestions based on name and description
    /// </summary>
    public async Task<List<ProductSuggestionDto>> SearchProductsForItem(
        string searchTerm,
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        var products = await context.Products
            .Where(p => 
                EF.Functions.Like(p.Name, $"%{searchTerm}%") ||
                (p.Description != null && EF.Functions.Like(p.Description, $"%{searchTerm}%")) ||
                (p.SKU != null && EF.Functions.Like(p.SKU, $"%{searchTerm}%")))
            .OrderBy(p => p.Name)
            .Take(50)
            .Select(p => new ProductSuggestionDto(
                p.Id,
                p.SKU,
                p.Name,
                p.Description,
                p.Quality,
                p.StockOnHand))
            .ToListAsync(cancellationToken);

        return products;
    }
}

/// <summary>
/// Staging purchase order detail with all items
/// </summary>
public record StagingPurchaseOrderDetailDto(
    Guid Id,
    string? SupplierReference,
    DateTimeOffset OrderDate,
    MyMarketManager.Data.Enums.ProcessingStatus Status,
    bool IsImported,
    string? ErrorMessage,
    string? SupplierName,
    List<StagingPurchaseOrderItemDto> Items);

/// <summary>
/// Staging purchase order item detail
/// </summary>
public record StagingPurchaseOrderItemDto(
    Guid Id,
    string Name,
    string? Description,
    string? SupplierReference,
    string? SupplierProductUrl,
    int Quantity,
    decimal ListedUnitPrice,
    decimal ActualUnitPrice,
    MyMarketManager.Data.Enums.CandidateStatus Status,
    bool IsImported,
    Guid? ProductId,
    LinkedProductDto? Product,
    Guid? SupplierId,
    string? SupplierName);

/// <summary>
/// Linked product information
/// </summary>
public record LinkedProductDto(
    Guid Id,
    string? SKU,
    string Name,
    MyMarketManager.Data.Enums.ProductQuality Quality);

/// <summary>
/// Product suggestion for matching
/// </summary>
public record ProductSuggestionDto(
    Guid Id,
    string? SKU,
    string Name,
    string? Description,
    MyMarketManager.Data.Enums.ProductQuality Quality,
    int StockOnHand);

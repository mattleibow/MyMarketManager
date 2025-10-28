using HotChocolate;
using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.WebApp.GraphQL;

/// <summary>
/// GraphQL queries for purchase orders
/// </summary>
[ExtendObjectType("Query")]
public class PurchaseOrderQueries
{
    /// <summary>
    /// Get all purchase orders
    /// </summary>
    public async Task<List<PurchaseOrderDto>> GetPurchaseOrders(
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Items)
            .OrderByDescending(po => po.OrderDate)
            .Select(po => new PurchaseOrderDto(
                po.Id,
                po.SupplierId,
                po.Supplier.Name,
                po.OrderDate,
                po.Status,
                po.Items.Count,
                po.ShippingFees,
                po.ImportFees,
                po.InsuranceFees,
                po.AdditionalFees,
                po.Notes))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get a purchase order by ID
    /// </summary>
    public async Task<PurchaseOrderDetailDto?> GetPurchaseOrderById(
        Guid id,
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        var po = await context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Items)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(po => po.Id == id, cancellationToken);

        if (po == null)
        {
            return null;
        }

        return new PurchaseOrderDetailDto(
            po.Id,
            po.SupplierId,
            po.Supplier.Name,
            po.OrderDate,
            po.Status,
            po.ShippingFees,
            po.ImportFees,
            po.InsuranceFees,
            po.AdditionalFees,
            po.Notes,
            po.Items.Select(item => new PurchaseOrderItemDto(
                item.Id,
                item.ProductId,
                item.Product?.Name,
                item.SupplierReference,
                item.SupplierProductUrl,
                item.Name,
                item.Description,
                item.Quantity,
                item.ListedUnitPrice,
                item.ActualUnitPrice,
                item.AllocatedUnitOverhead,
                item.TotalUnitCost)).ToList());
    }
}

/// <summary>
/// Purchase order summary for list view
/// </summary>
public record PurchaseOrderDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    DateTimeOffset OrderDate,
    MyMarketManager.Data.Enums.ProcessingStatus Status,
    int ItemCount,
    decimal ShippingFees,
    decimal ImportFees,
    decimal InsuranceFees,
    decimal AdditionalFees,
    string? Notes);

/// <summary>
/// Purchase order detail with items
/// </summary>
public record PurchaseOrderDetailDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    DateTimeOffset OrderDate,
    MyMarketManager.Data.Enums.ProcessingStatus Status,
    decimal ShippingFees,
    decimal ImportFees,
    decimal InsuranceFees,
    decimal AdditionalFees,
    string? Notes,
    List<PurchaseOrderItemDto> Items);

/// <summary>
/// Purchase order item
/// </summary>
public record PurchaseOrderItemDto(
    Guid Id,
    Guid? ProductId,
    string? ProductName,
    string? SupplierReference,
    string? SupplierProductUrl,
    string Name,
    string? Description,
    int Quantity,
    decimal ListedUnitPrice,
    decimal ActualUnitPrice,
    decimal AllocatedUnitOverhead,
    decimal TotalUnitCost);

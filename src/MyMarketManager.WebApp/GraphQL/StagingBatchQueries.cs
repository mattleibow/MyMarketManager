using HotChocolate;
using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.WebApp.GraphQL;

/// <summary>
/// GraphQL queries for staging batches
/// </summary>
[ExtendObjectType("Query")]
public class StagingBatchQueries
{
    /// <summary>
    /// Get all staging batches with their purchase orders
    /// </summary>
    public async Task<List<StagingBatchDto>> GetStagingBatches(
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.StagingBatches
            .Include(sb => sb.Supplier)
            .Include(sb => sb.StagingPurchaseOrders)
            .ThenInclude(spo => spo.Items)
            .OrderByDescending(sb => sb.StartedAt)
            .Select(sb => new StagingBatchDto(
                sb.Id,
                sb.BatchType,
                sb.BatchProcessorName,
                sb.SupplierId,
                sb.Supplier != null ? sb.Supplier.Name : null,
                sb.StartedAt,
                sb.CompletedAt,
                sb.Status,
                sb.Notes,
                sb.ErrorMessage,
                sb.StagingPurchaseOrders.Count,
                sb.StagingPurchaseOrders.Sum(spo => spo.Items.Count)))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get a staging batch by ID with details
    /// </summary>
    public async Task<StagingBatchDetailDto?> GetStagingBatchById(
        Guid id,
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        var batch = await context.StagingBatches
            .Include(sb => sb.Supplier)
            .Include(sb => sb.StagingPurchaseOrders)
            .ThenInclude(spo => spo.Items)
            .FirstOrDefaultAsync(sb => sb.Id == id, cancellationToken);

        if (batch == null)
        {
            return null;
        }

        return new StagingBatchDetailDto(
            batch.Id,
            batch.BatchType,
            batch.BatchProcessorName,
            batch.SupplierId,
            batch.Supplier?.Name,
            batch.StartedAt,
            batch.CompletedAt,
            batch.Status,
            batch.Notes,
            batch.ErrorMessage,
            batch.StagingPurchaseOrders.Select(spo => new StagingPurchaseOrderDto(
                spo.Id,
                spo.SupplierReference,
                spo.OrderDate,
                spo.Status,
                spo.IsImported,
                spo.ErrorMessage,
                spo.Items.Count)).ToList());
    }
}

/// <summary>
/// Staging batch summary for list view
/// </summary>
public record StagingBatchDto(
    Guid Id,
    MyMarketManager.Data.Enums.StagingBatchType BatchType,
    string? BatchProcessorName,
    Guid? SupplierId,
    string? SupplierName,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    MyMarketManager.Data.Enums.ProcessingStatus Status,
    string? Notes,
    string? ErrorMessage,
    int OrderCount,
    int ItemCount);

/// <summary>
/// Staging batch detail with purchase orders
/// </summary>
public record StagingBatchDetailDto(
    Guid Id,
    MyMarketManager.Data.Enums.StagingBatchType BatchType,
    string? BatchProcessorName,
    Guid? SupplierId,
    string? SupplierName,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    MyMarketManager.Data.Enums.ProcessingStatus Status,
    string? Notes,
    string? ErrorMessage,
    List<StagingPurchaseOrderDto> StagingPurchaseOrders);

/// <summary>
/// Staging purchase order summary
/// </summary>
public record StagingPurchaseOrderDto(
    Guid Id,
    string? SupplierReference,
    DateTimeOffset OrderDate,
    MyMarketManager.Data.Enums.ProcessingStatus Status,
    bool IsImported,
    string? ErrorMessage,
    int ItemCount);

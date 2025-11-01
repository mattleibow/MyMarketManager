using HotChocolate;
using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.GraphQL.Server;

/// <summary>
/// GraphQL mutations for staging purchase orders
/// </summary>
[ExtendObjectType("Mutation")]
public class StagingPurchaseOrderMutations
{
    /// <summary>
    /// Link a staging purchase order item to an existing product
    /// </summary>
    public async Task<LinkStagingItemResult> LinkStagingItemToProduct(
        LinkStagingItemToProductInput input,
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        var item = await context.StagingPurchaseOrderItems
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == input.StagingItemId, cancellationToken);

        if (item == null)
        {
            return new LinkStagingItemResult(false, "Staging item not found", null);
        }

        if (item.IsImported)
        {
            return new LinkStagingItemResult(false, "Cannot link item that has already been imported", null);
        }

        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == input.ProductId, cancellationToken);

        if (product == null)
        {
            return new LinkStagingItemResult(false, "Product not found", null);
        }

        // Link the item to the product
        item.ProductId = input.ProductId;
        item.Status = CandidateStatus.Linked;

        await context.SaveChangesAsync(cancellationToken);

        return new LinkStagingItemResult(
            true,
            null,
            new LinkedProductDto(
                product.Id,
                product.SKU,
                product.Name,
                product.Quality));
    }

    /// <summary>
    /// Unlink a staging purchase order item from a product
    /// </summary>
    public async Task<UnlinkStagingItemResult> UnlinkStagingItemFromProduct(
        Guid stagingItemId,
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        var item = await context.StagingPurchaseOrderItems
            .FirstOrDefaultAsync(i => i.Id == stagingItemId, cancellationToken);

        if (item == null)
        {
            return new UnlinkStagingItemResult(false, "Staging item not found");
        }

        if (item.IsImported)
        {
            return new UnlinkStagingItemResult(false, "Cannot unlink item that has already been imported");
        }

        // Unlink the item
        item.ProductId = null;
        
        // Only reset to Pending if it was Linked; preserve other statuses like Rejected
        if (item.Status == CandidateStatus.Linked)
        {
            item.Status = CandidateStatus.Pending;
        }

        await context.SaveChangesAsync(cancellationToken);

        return new UnlinkStagingItemResult(true, null);
    }
}

/// <summary>
/// Input for linking a staging item to a product
/// </summary>
public record LinkStagingItemToProductInput(
    Guid StagingItemId,
    Guid ProductId);

/// <summary>
/// Result of linking a staging item to a product
/// </summary>
public record LinkStagingItemResult(
    bool Success,
    string? ErrorMessage,
    LinkedProductDto? LinkedProduct);

/// <summary>
/// Result of unlinking a staging item from a product
/// </summary>
public record UnlinkStagingItemResult(
    bool Success,
    string? ErrorMessage);

/// <summary>
/// Linked product information
/// </summary>
public record LinkedProductDto(
    Guid Id,
    string? SKU,
    string Name,
    MyMarketManager.Data.Enums.ProductQuality Quality);

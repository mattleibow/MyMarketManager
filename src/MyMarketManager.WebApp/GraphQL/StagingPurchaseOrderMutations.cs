using HotChocolate;
using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.WebApp.GraphQL;

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

        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == input.ProductId, cancellationToken);

        if (product == null)
        {
            return new LinkStagingItemResult(false, "Product not found", null);
        }

        // Link the item to the product
        item.ProductId = input.ProductId;
        item.Status = CandidateStatus.Linked;
        item.UpdatedAt = DateTimeOffset.UtcNow;

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

        // Unlink the item
        item.ProductId = null;
        item.Status = CandidateStatus.Pending;
        item.UpdatedAt = DateTimeOffset.UtcNow;

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

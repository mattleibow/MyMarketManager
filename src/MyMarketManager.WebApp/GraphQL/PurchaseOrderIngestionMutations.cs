using HotChocolate;
using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Scrapers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MyMarketManager.WebApp.GraphQL;

/// <summary>
/// GraphQL mutations for PO ingestion
/// </summary>
[ExtendObjectType("Mutation")]
public class PurchaseOrderIngestionMutations
{
    /// <summary>
    /// Submit cookies for PO scraping
    /// </summary>
    public async Task<SubmitCookiesPayload> SubmitCookies(
        SubmitCookiesInput input,
        MyMarketManagerDbContext context,
        ILogger<PurchaseOrderIngestionMutations> logger,
        CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(input.CookieJson))
        {
            return new SubmitCookiesPayload
            {
                Success = false,
                Error = "Cookie JSON is required"
            };
        }

        if (string.IsNullOrWhiteSpace(input.ProcessorName))
        {
            return new SubmitCookiesPayload
            {
                Success = false,
                Error = "Processor name is required"
            };
        }

        // Validate JSON format
        try
        {
            JsonDocument.Parse(input.CookieJson);
        }
        catch
        {
            return new SubmitCookiesPayload
            {
                Success = false,
                Error = "Invalid JSON format"
            };
        }

        // Verify supplier exists
        var supplierExists = await context.Suppliers
            .AnyAsync(s => s.Id == input.SupplierId, cancellationToken);
        
        if (!supplierExists)
        {
            return new SubmitCookiesPayload
            {
                Success = false,
                Error = "Supplier not found"
            };
        }

        try
        {
            // Compute hash
            var cookieHash = ComputeHash(input.CookieJson);

            // Create staging batch
            var batch = new StagingBatch
            {
                Id = Guid.NewGuid(),
                BatchType = StagingBatchType.WebScrape,
                BatchProcessorName = input.ProcessorName,
                SupplierId = input.SupplierId,
                StartedAt = DateTimeOffset.UtcNow,
                FileHash = cookieHash,
                FileContents = input.CookieJson,
                Status = ProcessingStatus.Queued,
                Notes = $"Cookie submission on {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}"
            };

            context.StagingBatches.Add(batch);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created staging batch {BatchId} for supplier {SupplierId}",
                batch.Id, input.SupplierId);

            return new SubmitCookiesPayload
            {
                Success = true,
                BatchId = batch.Id,
                Message = $"Cookies submitted successfully! Scraping will begin within 5 minutes."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error submitting cookies");
            return new SubmitCookiesPayload
            {
                Success = false,
                Error = $"Error submitting cookies: {ex.Message}"
            };
        }
    }

    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

/// <summary>
/// Input for submitting cookies
/// </summary>
public record SubmitCookiesInput(
    Guid SupplierId,
    string ProcessorName,
    string CookieJson);

/// <summary>
/// Payload returned from submitting cookies
/// </summary>
public record SubmitCookiesPayload
{
    public bool Success { get; init; }
    public Guid? BatchId { get; init; }
    public string? Message { get; init; }
    public string? Error { get; init; }
}

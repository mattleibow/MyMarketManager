using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Services;

/// <summary>
/// Service responsible for processing pending staging batches.
/// </summary>
public class BatchIngestionProcessor
{
    private readonly MyMarketManagerDbContext _context;
    private readonly BlobStorageService _blobStorageService;
    private readonly ILogger<BatchIngestionProcessor> _logger;

    public BatchIngestionProcessor(
        MyMarketManagerDbContext context,
        BlobStorageService blobStorageService,
        ILogger<BatchIngestionProcessor> logger)
    {
        _context = context;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Processes all pending staging batches.
    /// </summary>
    public async Task<int> ProcessPendingBatchesAsync(CancellationToken cancellationToken = default)
    {
        // Find all pending batches
        var pendingBatches = await _context.StagingBatches
            .Where(b => b.Status == ProcessingStatus.Pending)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} pending batches to process", pendingBatches.Count);

        var processedCount = 0;

        foreach (var batch in pendingBatches)
        {
            try
            {
                _logger.LogInformation("Processing batch {BatchId}", batch.Id);
                await ProcessBatchAsync(batch, cancellationToken);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch {BatchId}", batch.Id);
                
                // Update batch status to indicate error
                batch.Status = ProcessingStatus.Partial;
                batch.Notes = $"Processing error: {ex.Message}";
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return processedCount;
    }

    /// <summary>
    /// Processes a single staging batch.
    /// </summary>
    public async Task ProcessBatchAsync(
        StagingBatch batch,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(batch.BlobStorageUrl))
        {
            _logger.LogWarning("Batch {BatchId} has no blob storage URL", batch.Id);
            batch.Status = ProcessingStatus.Partial;
            batch.Notes = "No blob storage URL provided";
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        // Download the blob
        using var blobStream = await _blobStorageService.DownloadFileAsync(batch.BlobStorageUrl, cancellationToken);
        using var memoryStream = new MemoryStream();
        await blobStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        // TODO: Extract ZIP file and parse supplier-specific data
        // For now, just mark as complete
        // In future iterations, this will:
        // 1. Extract password-protected ZIP
        // 2. Parse supplier data format
        // 3. Create StagingPurchaseOrder and StagingPurchaseOrderItem records

        batch.Status = ProcessingStatus.Complete;
        batch.Notes = $"Processed on {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}";
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Completed processing batch {BatchId}", batch.Id);
    }
}

using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Background service that processes pending staging batches.
/// Looks for batches with Status = Pending and processes them.
/// </summary>
public class BlobIngestionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BlobIngestionService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(5);

    public BlobIngestionService(
        IServiceProvider serviceProvider,
        ILogger<BlobIngestionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Blob ingestion service started");

        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingBatchesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending batches");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("Blob ingestion service stopped");
    }

    private async Task ProcessPendingBatchesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var blobService = scope.ServiceProvider.GetRequiredService<BlobStorageService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyMarketManagerDbContext>();

        // Find all pending batches
        var pendingBatches = await dbContext.StagingBatches
            .Where(b => b.Status == ProcessingStatus.Pending)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} pending batches to process", pendingBatches.Count);

        foreach (var batch in pendingBatches)
        {
            try
            {
                _logger.LogInformation("Processing batch {BatchId}", batch.Id);
                await ProcessBatchAsync(batch, blobService, dbContext, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch {BatchId}", batch.Id);
                
                // Update batch status to indicate error
                batch.Status = ProcessingStatus.Partial;
                batch.Notes = $"Processing error: {ex.Message}";
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task ProcessBatchAsync(
        StagingBatch batch,
        BlobStorageService blobService,
        MyMarketManagerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(batch.BlobStorageUrl))
        {
            _logger.LogWarning("Batch {BatchId} has no blob storage URL", batch.Id);
            batch.Status = ProcessingStatus.Partial;
            batch.Notes = "No blob storage URL provided";
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        // Download the blob
        using var blobStream = await blobService.DownloadFileAsync(batch.BlobStorageUrl, cancellationToken);
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
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Completed processing batch {BatchId}", batch.Id);
    }
}

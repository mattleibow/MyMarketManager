using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Background service that monitors blob storage for new supplier data files
/// and processes them by creating StagingBatch records.
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
                await ProcessNewBlobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing blobs");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("Blob ingestion service stopped");
    }

    private async Task ProcessNewBlobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var blobService = scope.ServiceProvider.GetRequiredService<BlobStorageService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyMarketManagerDbContext>();

        var blobs = await blobService.ListBlobsAsync(cancellationToken);
        _logger.LogInformation("Found {Count} blobs in storage", blobs.Count);

        foreach (var blob in blobs)
        {
            try
            {
                var blobUrl = blobService.GetBlobUrl(blob.Name);

                // Check if this blob has already been processed by checking the URL
                var existingBatch = await dbContext.StagingBatches
                    .FirstOrDefaultAsync(b => b.BlobStorageUrl == blobUrl, cancellationToken);

                if (existingBatch != null)
                {
                    _logger.LogDebug("Blob {BlobName} already processed", blob.Name);
                    continue;
                }

                _logger.LogInformation("Processing new blob: {BlobName}", blob.Name);
                await ProcessBlobAsync(blobUrl, blob.Name, blobService, dbContext, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing blob {BlobName}", blob.Name);
            }
        }
    }

    private async Task ProcessBlobAsync(
        string blobUrl,
        string blobName,
        BlobStorageService blobService,
        MyMarketManagerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Download the blob
        using var blobStream = await blobService.DownloadFileAsync(blobUrl, cancellationToken);
        using var memoryStream = new MemoryStream();
        await blobStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        // Calculate file hash for deduplication
        var fileHash = await ComputeFileHashAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        // Check if file hash already exists
        var existingBatchByHash = await dbContext.StagingBatches
            .FirstOrDefaultAsync(b => b.FileHash == fileHash, cancellationToken);

        if (existingBatchByHash != null)
        {
            _logger.LogWarning(
                "File {BlobName} has same hash as existing batch {BatchId}, skipping",
                blobName,
                existingBatchByHash.Id);
            return;
        }

        // For now, we'll create a basic staging batch
        // In a real implementation, we would extract and parse the ZIP file here
        // For Shein, the file is password-protected, so that logic would go here

        // Get or create a default supplier for testing
        // In production, we would need to identify the supplier from the file or metadata
        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(cancellationToken);
        if (supplier == null)
        {
            supplier = new Supplier
            {
                Id = Guid.NewGuid(),
                Name = "Default Supplier"
            };
            dbContext.Suppliers.Add(supplier);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Create the staging batch
        var stagingBatch = new StagingBatch
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier.Id,
            UploadDate = DateTimeOffset.UtcNow,
            FileHash = fileHash,
            BlobStorageUrl = blobUrl,
            Status = ProcessingStatus.Pending,
            Notes = $"Imported from blob: {blobName}"
        };

        dbContext.StagingBatches.Add(stagingBatch);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created staging batch {BatchId} for blob {BlobName}",
            stagingBatch.Id,
            blobName);
    }

    private static async Task<string> ComputeFileHashAsync(Stream stream, CancellationToken cancellationToken)
    {
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}

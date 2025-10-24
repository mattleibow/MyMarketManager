using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Scrapers;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Background service that processes queued staging batches.
/// Routes batches to appropriate processors based on BatchType and BatchProcessorName.
/// </summary>
public class IngestionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IngestionService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(5);

    public IngestionService(
        IServiceProvider serviceProvider,
        ILogger<IngestionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ingestion service started");

        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending ingestion batches");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("Ingestion service stopped");
    }

    private async Task ProcessBatchesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyMarketManagerDbContext>();
        
        // Get all queued batches
        var queuedBatches = await context.StagingBatches
            .Where(b => b.Status == ProcessingStatus.Queued)
            .Include(b => b.Supplier)
            .OrderBy(b => b.StartedAt)
            .ToListAsync(cancellationToken);

        if (queuedBatches.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Found {Count} queued batches to process", queuedBatches.Count);

        foreach (var batch in queuedBatches)
        {
            try
            {
                await ProcessBatchAsync(scope.ServiceProvider, batch, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch {BatchId}", batch.Id);
                
                // Update batch status to indicate error
                batch.Status = ProcessingStatus.Failed;
                batch.ErrorMessage = $"Processing error: {ex.Message}";
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task ProcessBatchAsync(
        IServiceProvider serviceProvider,
        StagingBatch batch,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing batch {BatchId} - Type: {BatchType}, Processor: {ProcessorName}",
            batch.Id,
            batch.BatchType,
            batch.BatchProcessorName ?? "none");

        // Route based on batch type
        switch (batch.BatchType)
        {
            case StagingBatchType.WebScrape:
                await ProcessWebScrapeBatchAsync(serviceProvider, batch, cancellationToken);
                break;

            // Future batch types can be added here:
            // case StagingBatchType.BlobUpload:
            //     await ProcessBlobUploadBatchAsync(serviceProvider, batch, cancellationToken);
            //     break;

            default:
                _logger.LogWarning("Unknown batch type {BatchType} for batch {BatchId}", batch.BatchType, batch.Id);
                batch.Status = ProcessingStatus.Failed;
                batch.ErrorMessage = $"Unknown batch type: {batch.BatchType}";
                var context = serviceProvider.GetRequiredService<MyMarketManagerDbContext>();
                await context.SaveChangesAsync(cancellationToken);
                break;
        }
    }

    private async Task ProcessWebScrapeBatchAsync(
        IServiceProvider serviceProvider,
        StagingBatch batch,
        CancellationToken cancellationToken)
    {
        var context = serviceProvider.GetRequiredService<MyMarketManagerDbContext>();

        // Validate batch data
        if (batch.SupplierId == null)
        {
            _logger.LogWarning("Batch {BatchId} has no supplier ID", batch.Id);
            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = "No supplier ID provided";
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (string.IsNullOrEmpty(batch.FileContents))
        {
            _logger.LogWarning("Batch {BatchId} has no cookie data", batch.Id);
            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = "No cookie data provided";
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (string.IsNullOrEmpty(batch.BatchProcessorName))
        {
            _logger.LogWarning("No processor name found in batch {BatchId}", batch.Id);
            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = "No processor name specified";
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        // Get the supplier
        var supplier = await context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == batch.SupplierId, cancellationToken);

        if (supplier == null)
        {
            _logger.LogWarning("Supplier {SupplierId} not found for batch {BatchId}", 
                batch.SupplierId, batch.Id);
            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = "Supplier not found";
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        // Update status to Started
        batch.Status = ProcessingStatus.Started;
        batch.StartedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        // Get the web scraper using keyed service
        var scraper = serviceProvider.GetKeyedService<WebScraper>(batch.BatchProcessorName);
        
        if (scraper == null)
        {
            _logger.LogWarning("No scraper found for processor name {ProcessorName}", batch.BatchProcessorName);
            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = $"No scraper registered for: {batch.BatchProcessorName}";
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        _logger.LogInformation("Running {ProcessorName} scraper for batch {BatchId}", batch.BatchProcessorName, batch.Id);

        // Run the scraper with the existing batch
        await scraper.ScrapeBatchAsync(batch, cancellationToken);

        // Mark as complete
        batch.Status = ProcessingStatus.Completed;
        batch.CompletedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Completed processing batch {BatchId}", batch.Id);
    }
}

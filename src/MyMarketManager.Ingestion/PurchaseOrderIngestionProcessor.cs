using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;
using MyMarketManager.Scrapers;
using MyMarketManager.Scrapers.Core;

namespace MyMarketManager.Ingestion;

/// <summary>
/// Processor responsible for purchase order ingestion batches via web scraping.
/// Processes a single batch at a time to avoid consuming all processing time.
/// </summary>
public class PurchaseOrderIngestionProcessor : IIngestionProcessor
{
    private readonly MyMarketManagerDbContext _context;
    private readonly ILogger<PurchaseOrderIngestionProcessor> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PurchaseOrderIngestionProcessor(
        MyMarketManagerDbContext context,
        ILogger<PurchaseOrderIngestionProcessor> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Determines if this processor can handle the given batch.
    /// This processor handles WebScrape batches.
    /// </summary>
    public bool CanProcess(StagingBatch batch)
    {
        return batch.BatchType == StagingBatchType.WebScrape 
            && batch.Status == ProcessingStatus.Queued;
    }

    /// <summary>
    /// Processes a single staging batch by running the appropriate scraper.
    /// </summary>
    public async Task ProcessBatchAsync(
        StagingBatch batch,
        CancellationToken cancellationToken = default)
    {
        if (batch.SupplierId == null)
        {
            _logger.LogWarning("Batch {BatchId} has no supplier ID", batch.Id);
            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = "No supplier ID provided";
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (string.IsNullOrEmpty(batch.FileContents))
        {
            _logger.LogWarning("Batch {BatchId} has no cookie data", batch.Id);
            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = "No cookie data provided";
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        // Get the supplier to determine which scraper to use
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == batch.SupplierId, cancellationToken);

        if (supplier == null)
        {
            _logger.LogWarning("Supplier {SupplierId} not found for batch {BatchId}", 
                batch.SupplierId, batch.Id);
            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = "Supplier not found";
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        // Update status to Started
        batch.Status = ProcessingStatus.Started;
        batch.StartedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // Get the processor name from batch
        var processorName = batch.BatchProcessorName;
        if (string.IsNullOrEmpty(processorName))
        {
            _logger.LogWarning("No processor name found in batch {BatchId}", batch.Id);
            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = "No processor name specified";
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            // Create scraper instance using factory
            using var scope = _serviceProvider.CreateScope();
            var scraperFactory = scope.ServiceProvider.GetRequiredService<IWebScraperFactory>();
            var scraper = scraperFactory.CreateScraper(processorName);

            _logger.LogInformation("Running {ProcessorName} scraper for batch {BatchId}", processorName, batch.Id);

            // Run the scraper with the existing batch
            await scraper.ScrapeBatchAsync(batch, cancellationToken);

            // Mark as complete
            batch.Status = ProcessingStatus.Completed;
            batch.CompletedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Completed processing batch {BatchId}", batch.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running scraper for batch {BatchId}", batch.Id);
            batch.Status = ProcessingStatus.Failed;
            batch.ErrorMessage = ex.Message;
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}

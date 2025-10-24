using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Services;

/// <summary>
/// Service responsible for processing pending PO ingestion batches via web scraping.
/// Contains testable business logic separated from the background service.
/// </summary>
public class PoIngestionProcessor
{
    private readonly MyMarketManagerDbContext _context;
    private readonly ILogger<PoIngestionProcessor> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PoIngestionProcessor(
        MyMarketManagerDbContext context,
        ILogger<PoIngestionProcessor> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Processes all pending web scrape batches.
    /// </summary>
    public async Task<int> ProcessPendingBatchesAsync(CancellationToken cancellationToken = default)
    {
        // Find all Queued WebScrape batches
        var pendingBatches = await _context.StagingBatches
            .Where(b => b.BatchType == StagingBatchType.WebScrape && b.Status == ProcessingStatus.Queued)
            .Include(b => b.Supplier)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} pending web scrape batches to process", pendingBatches.Count);

        var processedCount = 0;

        foreach (var batch in pendingBatches)
        {
            try
            {
                _logger.LogInformation("Processing batch {BatchId} for supplier {SupplierName}", 
                    batch.Id, batch.Supplier?.Name ?? "unknown");
                
                await ProcessBatchAsync(batch, cancellationToken);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch {BatchId}", batch.Id);
                
                // Update batch status to indicate error
                batch.Status = ProcessingStatus.Failed;
                batch.ErrorMessage = $"Processing error: {ex.Message}";
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return processedCount;
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

        // TODO: Add scraper factory/registry to dynamically select scraper based on supplier
        // For now, we'll assume Shein for all suppliers
        // In future: if (supplier.Name.Contains("Shein", StringComparison.OrdinalIgnoreCase)) { ... }

        try
        {
            // Create scraper instance using DI
            // Note: Scrapers are registered in WebApp's Program.cs
            // This is a placeholder - actual implementation will depend on scraper registry
            _logger.LogInformation("Scraper processing for batch {BatchId} will be implemented", batch.Id);

            // For now, mark as complete - actual scraping will be added in next iteration
            batch.Status = ProcessingStatus.Completed;
            batch.CompletedAt = DateTimeOffset.UtcNow;
            batch.Notes = $"Processed on {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}";
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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyMarketManager.Data;
using MyMarketManager.Data.Enums;
using MyMarketManager.Scrapers;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Background service that processes queued staging batches.
/// Simply gets batches and delegates to appropriate processors.
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
        var factory = scope.ServiceProvider.GetRequiredService<BatchProcessorFactory>();
        
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
                // Skip batches without processor name
                if (string.IsNullOrEmpty(batch.BatchProcessorName))
                {
                    _logger.LogWarning("Batch {BatchId} has no processor name", batch.Id);
                    continue;
                }

                // Get the processor
                var processor = factory.GetProcessor(batch.BatchType, batch.BatchProcessorName);
                
                if (processor == null)
                {
                    _logger.LogWarning(
                        "No processor found for batch {BatchId} - Type: {BatchType}, Name: {ProcessorName}",
                        batch.Id,
                        batch.BatchType,
                        batch.BatchProcessorName);
                    continue;
                }

                // Let the processor handle everything
                _logger.LogInformation(
                    "Starting processor for batch {BatchId} - Type: {BatchType}, Name: {ProcessorName}",
                    batch.Id,
                    batch.BatchType,
                    batch.BatchProcessorName);

                await processor.ScrapeBatchAsync(batch, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch {BatchId}", batch.Id);
            }
        }
    }
}

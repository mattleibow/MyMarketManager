using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyMarketManager.Data;
using MyMarketManager.Data.Enums;
using MyMarketManager.Ingestion;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Generic background service that processes staging batches using registered ingestion processors.
/// Gets processors directly from DI and processes one batch at a time per processor.
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

        // Get all available processors directly from DI
        var processors = scope.ServiceProvider.GetServices<IIngestionProcessor>().ToList();

        if (processors.Count == 0)
        {
            _logger.LogWarning("No ingestion processors registered");
            return;
        }

        var processedCount = 0;

        // Process one batch per processor type to avoid one processor consuming all time
        foreach (var processor in processors)
        {
            // Find the first batch that this processor can handle
            var batch = queuedBatches.FirstOrDefault(b => processor.CanProcess(b));

            if (batch == null)
            {
                continue;
            }

            try
            {
                _logger.LogInformation(
                    "Processing batch {BatchId} with processor {ProcessorType}",
                    batch.Id,
                    processor.GetType().Name);

                await processor.ProcessBatchAsync(batch, cancellationToken);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing batch {BatchId} with processor {ProcessorType}",
                    batch.Id,
                    processor.GetType().Name);

                // Update batch status to indicate error
                batch.Status = ProcessingStatus.Failed;
                batch.ErrorMessage = $"Processing error: {ex.Message}";
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        if (processedCount > 0)
        {
            _logger.LogInformation("Processed {Count} ingestion batches", processedCount);
        }
    }
}

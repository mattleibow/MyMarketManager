using Microsoft.Extensions.Options;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Background service that processes queued staging batches.
/// Simply gets batches and delegates to appropriate processors.
/// </summary>
/// <remarks>
/// This service has been replaced by <see cref="UnifiedBackgroundProcessingService"/> which uses
/// a Channel-based work item processing system. The new system:
/// - Allows processors to fetch their own work items
/// - Prevents starvation with bounded channels
/// - Supports multiple work item types (StagingBatch, ImageVectorization, cleanup, etc.)
/// - Uses a single background service for all processing
/// </remarks>
[Obsolete("Use UnifiedBackgroundProcessingService with IWorkItemHandler instead. This will be removed in a future version.")]
public class IngestionService(
    IServiceProvider serviceProvider,
    ILogger<IngestionService> logger,
    IOptions<IngestionServiceOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Ingestion service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<BatchProcessingService>();

                await service.ProcessBatchesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing pending ingestion batches");
            }

            await Task.Delay(options.Value.PollInterval, stoppingToken);
        }

        logger.LogInformation("Ingestion service stopped");
    }
}

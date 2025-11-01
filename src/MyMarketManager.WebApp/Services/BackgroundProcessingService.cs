using Microsoft.Extensions.Options;
using MyMarketManager.Data.Processing;
using MyMarketManager.Data.Services;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Unified background service that handles all periodic processing tasks using the generic work item framework.
/// Supports:
/// - Batch processing (ingestion, web scraping, etc.) via StagingBatch
/// - Image vectorization via ImageVectorizationWorkItem
/// - Future processing tasks can be added by registering new work item processors
/// </summary>
public class BackgroundProcessingService(
    IServiceProvider serviceProvider,
    ILogger<BackgroundProcessingService> logger,
    IOptions<BackgroundProcessingServiceOptions> options,
    IBatchProcessorFactory processorFactory) : BackgroundService
{
    private readonly BackgroundProcessingServiceOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Background processing service started");

        // Track last execution times for each processor type
        var lastBatchProcessing = DateTimeOffset.MinValue;
        var lastVectorization = DateTimeOffset.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            try
            {
                // Process batches if interval has elapsed
                if (now - lastBatchProcessing >= _options.BatchProcessingInterval)
                {
                    logger.LogInformation("Running batch processing");
                    await ProcessBatchesAsync(stoppingToken);
                    lastBatchProcessing = now;
                }

                // Process image vectorization if interval has elapsed
                if (now - lastVectorization >= _options.ImageVectorizationInterval)
                {
                    logger.LogInformation("Running image vectorization");
                    await ProcessImageVectorizationAsync(stoppingToken);
                    lastVectorization = now;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in background processing");
            }

            // Calculate wait time until next processing run
            // We wait for whichever comes first: batch processing or vectorization
            var timeUntilNextBatch = lastBatchProcessing + _options.BatchProcessingInterval - now;
            var timeUntilNextVectorization = lastVectorization + _options.ImageVectorizationInterval - now;
            var shortestWait = Math.Min(timeUntilNextBatch.TotalSeconds, timeUntilNextVectorization.TotalSeconds);
            var waitTime = TimeSpan.FromSeconds(Math.Max(1, shortestWait)); // At least 1 second

            await Task.Delay(waitTime, stoppingToken);
        }

        logger.LogInformation("Background processing service stopped");
    }

    private async Task ProcessBatchesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<BatchProcessingService>();
            await service.ProcessBatchesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing batches");
        }
    }

    private async Task ProcessImageVectorizationAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Create a work item for this vectorization run
            var workItem = new ImageVectorizationWorkItem(ProcessorNames.ImageVectorization);

            // Get the processor from the factory
            var processor = processorFactory.GetWorkItemProcessor(ProcessorNames.ImageVectorization, typeof(ImageVectorizationWorkItem)) 
                as IWorkItemProcessor<ImageVectorizationWorkItem>;

            if (processor == null)
            {
                logger.LogWarning("Image vectorization processor '{ProcessorName}' not registered", ProcessorNames.ImageVectorization);
                return;
            }

            // Process the work item
            await processor.ProcessAsync(workItem, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing image vectorization");
        }
    }
}

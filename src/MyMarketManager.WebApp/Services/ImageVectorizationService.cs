using Microsoft.Extensions.Options;
using MyMarketManager.Data.Services;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Background service that periodically processes product images for AI analysis and vectorization.
/// Simply polls for pending images and delegates to the processor.
/// </summary>
/// <remarks>
/// This service has been replaced by <see cref="UnifiedBackgroundProcessingService"/> which uses
/// the <see cref="ImageVectorizationHandler"/> work item handler. The new system:
/// - Allows processors to fetch their own work items
/// - Prevents starvation with bounded channels  
/// - Supports multiple work item types
/// - Can be easily extended to handle multiple sources (product photos, delivery photos, etc.)
/// </remarks>
[Obsolete("Use UnifiedBackgroundProcessingService with ImageVectorizationHandler instead. This will be removed in a future version.")]
public class ImageVectorizationService(
    IServiceProvider serviceProvider,
    ILogger<ImageVectorizationService> logger,
    IOptions<ImageVectorizationServiceOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Image vectorization service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ImageVectorizationProcessor>();

                await processor.ProcessPendingImagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing pending images");
            }

            await Task.Delay(options.Value.PollInterval, stoppingToken);
        }

        logger.LogInformation("Image vectorization service stopped");
    }
}

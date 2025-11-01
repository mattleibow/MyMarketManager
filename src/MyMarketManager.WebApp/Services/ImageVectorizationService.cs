using Microsoft.Extensions.Options;
using MyMarketManager.Data.Services;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Background service that periodically processes product images for AI analysis and vectorization.
/// Simply polls for pending images and delegates to the processor.
/// </summary>
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

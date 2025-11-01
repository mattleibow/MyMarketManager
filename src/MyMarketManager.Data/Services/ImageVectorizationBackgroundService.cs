using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MyMarketManager.Data.Services;

/// <summary>
/// Background service that periodically processes product images for AI analysis and vectorization.
/// Runs every 10 minutes to check for new images without vector embeddings.
/// </summary>
public class ImageVectorizationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImageVectorizationBackgroundService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(10);

    public ImageVectorizationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ImageVectorizationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Image vectorization service started");

        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessImagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing images");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("Image vectorization service stopped");
    }

    private async Task ProcessImagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<ImageVectorizationProcessor>();

        try
        {
            var processedCount = await processor.ProcessPendingImagesAsync(cancellationToken);

            if (processedCount > 0)
            {
                _logger.LogInformation("Processed {Count} images", processedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in image processing batch");
        }
    }
}

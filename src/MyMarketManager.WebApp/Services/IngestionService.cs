using Microsoft.Extensions.Options;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Background service that processes queued staging batches.
/// Simply gets batches and delegates to appropriate processors.
/// </summary>
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

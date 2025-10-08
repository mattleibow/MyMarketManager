using MyMarketManager.Data.Services;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Background service that processes pending staging batches every 5 minutes.
/// </summary>
public class BlobIngestionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BlobIngestionService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(5);

    public BlobIngestionService(
        IServiceProvider serviceProvider,
        ILogger<BlobIngestionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Blob ingestion service started");

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
                _logger.LogError(ex, "Error processing pending batches");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("Blob ingestion service stopped");
    }

    private async Task ProcessBatchesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var blobService = scope.ServiceProvider.GetRequiredService<BlobStorageService>();
        var processor = scope.ServiceProvider.GetRequiredService<BatchIngestionProcessor>();

        var processedCount = await processor.ProcessPendingBatchesAsync(
            blobService.DownloadFileAsync,
            cancellationToken);

        if (processedCount > 0)
        {
            _logger.LogInformation("Processed {Count} batches", processedCount);
        }
    }
}

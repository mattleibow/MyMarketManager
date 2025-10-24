using MyMarketManager.Ingestion;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Background service that processes pending PO ingestion batches via web scraping.
/// This service is minimal and delegates all business logic to PoIngestionProcessor.
/// Follows the same pattern as DatabaseMigrationService.
/// </summary>
public class PoIngestionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PoIngestionService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(5);

    public PoIngestionService(
        IServiceProvider serviceProvider,
        ILogger<PoIngestionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PO ingestion service started");

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
                _logger.LogError(ex, "Error processing pending PO ingestion batches");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("PO ingestion service stopped");
    }

    private async Task ProcessBatchesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<PoIngestionProcessor>();

        var processedCount = await processor.ProcessPendingBatchesAsync(cancellationToken);

        if (processedCount > 0)
        {
            _logger.LogInformation("Processed {Count} PO ingestion batches", processedCount);
        }
    }
}

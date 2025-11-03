using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MyMarketManager.Processing;

/// <summary>
/// Unified background service that orchestrates all work item processing.
/// Uses the WorkItemProcessingService with Channel-based queuing.
/// </summary>
public class BackgroundProcessingService(
    WorkItemProcessingService processingService,
    ILogger<BackgroundProcessingService> logger,
    IOptions<BackgroundProcessingOptions> options) : BackgroundService
{
    private readonly WorkItemProcessingService _processingService = processingService ?? throw new ArgumentNullException(nameof(processingService));
    private readonly ILogger<BackgroundProcessingService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly BackgroundProcessingOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Unified background processing service started with {Interval} poll interval", 
            _options.PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _processingService.ProcessCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in processing cycle");
            }

            // Wait before next cycle
            await Task.Delay(_options.PollInterval, stoppingToken);
        }

        _logger.LogInformation("Unified background processing service stopped");
    }
}

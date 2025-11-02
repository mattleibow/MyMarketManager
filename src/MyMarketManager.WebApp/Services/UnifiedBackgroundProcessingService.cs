using Microsoft.Extensions.Options;
using MyMarketManager.Data.Processing;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Configuration for the unified background processing service.
/// </summary>
public class UnifiedBackgroundProcessingOptions
{
    /// <summary>
    /// Interval between processing cycles.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Unified background service that orchestrates all work item processing.
/// Uses the WorkItemProcessingEngine with Channel-based queuing.
/// </summary>
public class UnifiedBackgroundProcessingService : BackgroundService
{
    private readonly WorkItemProcessingEngine _engine;
    private readonly ILogger<UnifiedBackgroundProcessingService> _logger;
    private readonly UnifiedBackgroundProcessingOptions _options;

    public UnifiedBackgroundProcessingService(
        WorkItemProcessingEngine engine,
        ILogger<UnifiedBackgroundProcessingService> logger,
        IOptions<UnifiedBackgroundProcessingOptions> options)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initialize the engine with all registered handlers
        _engine.Initialize();

        _logger.LogInformation("Unified background processing service started with {Interval} poll interval", 
            _options.PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _engine.ProcessCycleAsync(stoppingToken);
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

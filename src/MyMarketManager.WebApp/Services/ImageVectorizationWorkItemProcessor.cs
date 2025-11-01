using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data.Processing;
using MyMarketManager.Data.Services;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Work item processor for image vectorization.
/// Processes ImageVectorizationWorkItem by delegating to ImageVectorizationProcessor.
/// </summary>
public class ImageVectorizationWorkItemProcessor : IWorkItemProcessor<ImageVectorizationWorkItem>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImageVectorizationWorkItemProcessor> _logger;

    public ImageVectorizationWorkItemProcessor(
        IServiceProvider serviceProvider,
        ILogger<ImageVectorizationWorkItemProcessor> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ProcessAsync(ImageVectorizationWorkItem workItem, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting image vectorization for work item {WorkItemId}", workItem.Id);

        try
        {
            // Create a scope to get the ImageVectorizationProcessor
            using var scope = _serviceProvider.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<ImageVectorizationProcessor>();

            // Process all pending images
            await processor.ProcessPendingImagesAsync(cancellationToken);

            _logger.LogInformation("Image vectorization completed successfully for work item {WorkItemId}", workItem.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during image vectorization for work item {WorkItemId}", workItem.Id);
            throw;
        }
    }
}

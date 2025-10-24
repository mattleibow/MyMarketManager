using Microsoft.Extensions.DependencyInjection;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Processing;

public static class Extensions
{
    public static IBatchProcessorFactoryBuilder AddBatchProcessorFactory(this IServiceCollection services)
    {
        // Register the factory
        services.AddScoped<IBatchProcessorFactory, BatchProcessorFactory>();
        
        // Configure empty options initially
        services.Configure<BatchProcessorOptions>(options => { });

        return new BatchProcessorFactoryBuilder(services);
    }

    /// <summary>
    /// Registers a web scraper processor.
    /// </summary>
    public static IBatchProcessorFactoryBuilder AddWebScraper<TProcessor>(this IBatchProcessorFactoryBuilder builder, string processorName)
        where TProcessor : class, IBatchProcessor =>
        builder.AddProcessor<TProcessor>(StagingBatchType.WebScrape, processorName);

    /// <summary>
    /// Registers a blob storage processor.
    /// </summary>
    public static IBatchProcessorFactoryBuilder AddBlobStorageProcessor<TProcessor>(this IBatchProcessorFactoryBuilder builder, string processorName)
        where TProcessor : class, IBatchProcessor =>
        builder.AddProcessor<TProcessor>(StagingBatchType.BlobUpload, processorName);
}

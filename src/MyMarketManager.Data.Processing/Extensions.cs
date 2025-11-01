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
    public static IBatchProcessorFactoryBuilder AddWebScraper<TProcessor>(
        this IBatchProcessorFactoryBuilder builder,
        string processorName,
        ProcessorPurpose purpose = ProcessorPurpose.Ingestion,
        string? displayName = null,
        string? description = null)
        where TProcessor : class, IBatchProcessor =>
        builder.AddProcessor<TProcessor>(StagingBatchType.WebScrape, processorName, purpose, displayName, description);

    /// <summary>
    /// Registers a blob storage processor.
    /// </summary>
    public static IBatchProcessorFactoryBuilder AddBlobStorageProcessor<TProcessor>(
        this IBatchProcessorFactoryBuilder builder,
        string processorName,
        ProcessorPurpose purpose = ProcessorPurpose.Ingestion,
        string? displayName = null,
        string? description = null)
        where TProcessor : class, IBatchProcessor =>
        builder.AddProcessor<TProcessor>(StagingBatchType.BlobUpload, processorName, purpose, displayName, description);

    /// <summary>
    /// Registers an image vectorization processor.
    /// </summary>
    public static IBatchProcessorFactoryBuilder AddImageVectorization<TProcessor>(
        this IBatchProcessorFactoryBuilder builder,
        string processorName = "ImageVectorization",
        string? displayName = null,
        string? description = null)
        where TProcessor : class, IWorkItemProcessor<ImageVectorizationWorkItem> =>
        builder.AddWorkItemProcessor<TProcessor, ImageVectorizationWorkItem>(
            processorName,
            ProcessorPurpose.Internal,
            displayName ?? "Image Vectorization",
            description ?? "Generates vector embeddings for product images");
}

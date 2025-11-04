using Microsoft.Extensions.DependencyInjection;

namespace MyMarketManager.Processing;

internal class BackgroundProcessingBuilder(IServiceCollection services) : IBackgroundProcessingBuilder
{
    public IServiceCollection Services { get; } = services;

    public IBackgroundProcessingBuilder AddHandler<THandler>(
        string name,
        int maxItemsPerCycle = 10,
        WorkItemHandlerPurpose purpose = WorkItemHandlerPurpose.Internal)
        where THandler : class, IWorkItemHandler
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxItemsPerCycle, 1);

        // Register the handler
        Services.AddScoped<THandler>();

        // Register handler configuration using IOptions pattern
        Services.Configure<WorkItemProcessingServiceOptions>(options =>
        {
            options.Register<THandler>(name, maxItemsPerCycle, purpose);
        });

        return this;
    }
}

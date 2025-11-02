using Microsoft.Extensions.DependencyInjection;

namespace MyMarketManager.Data.Processing;

internal class WorkItemProcessingBuilder : IWorkItemProcessingBuilder
{
    public IServiceCollection Services { get; }

    public WorkItemProcessingBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IWorkItemProcessingBuilder AddHandler<THandler, TWorkItem>(
        string name,
        int maxItemsPerCycle = 10,
        ProcessorPurpose purpose = ProcessorPurpose.Internal)
        where THandler : class, IWorkItemHandler<TWorkItem>
        where TWorkItem : IWorkItem
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxItemsPerCycle, 1);

        // Register the handler
        Services.AddScoped<THandler>();

        // Register handler configuration using IOptions pattern
        Services.Configure<WorkItemProcessingEngineOptions>(options =>
        {
            options.Registrations.Add(new WorkItemProcessingEngineOptions.HandlerRegistration(
                typeof(THandler),
                typeof(TWorkItem),
                name,
                maxItemsPerCycle,
                purpose));
        });

        return this;
    }
}

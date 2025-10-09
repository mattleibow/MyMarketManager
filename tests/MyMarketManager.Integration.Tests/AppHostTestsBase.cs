using Aspire.Hosting;
using Microsoft.Extensions.Logging;

namespace MyMarketManager.Integration.Tests;

public abstract class AppHostTestsBase(ITestOutputHelper outputHelper) : IAsyncLifetime
{
    protected static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    protected DistributedApplication App { get; private set; } = null!;

    protected CancellationToken Cancel => TestContext.Current.CancellationToken;

    public virtual async ValueTask InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MyMarketManager_AppHost>(["UseVolumes=False", "UseSqliteDatabase=True"], Cancel);

        // Log everything for the tests
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);

            // Override the logging filters from the app's configuration
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);

            // Add the xUnit output helper
            logging.AddXUnit(outputHelper);
        });

        // Configure HTTP clients to use standard resilience handlers
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        App = await appHost
            .BuildAsync(Cancel)
            .WaitAsync(DefaultTimeout, Cancel);

        await App
            .StartAsync(Cancel)
            .WaitAsync(DefaultTimeout, Cancel);

    }

    public virtual async ValueTask DisposeAsync()
    {
        if (App is not null)
        {
            await App.DisposeAsync();
        }
    }
}

using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using MyMarketManager.Tests.Shared;
using Polly;

namespace MyMarketManager.Integration.Tests;

public abstract class AppHostTestsBase(ITestOutputHelper outputHelper) : IAsyncLifetime
{
    private readonly SqlServerHelper _sqlServer = new(outputHelper);

    protected DistributedApplication App { get; private set; } = null!;

    protected CancellationToken Cancel => TestContext.Current.CancellationToken;

    public virtual async ValueTask InitializeAsync()
    {
        // Configure DCP to prefer IPv4 to avoid issues in environments where IPv6 is disabled
        // (e.g., when DOTNET_SYSTEM_NET_DISABLEIPV6=1 is set)
        Environment.SetEnvironmentVariable("DCP_IP_VERSION_PREFERENCE", "ipv4");

        var builderStepTimeout = TimeSpan.FromMinutes(10);

        var connectionString = await _sqlServer.ConnectAsync();

        // Pass the connection string to AppHost
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MyMarketManager_AppHost>(
            [
                "UseVolumes=False",
                $"UseDatabaseConnectionString={connectionString}"
            ],
            Cancel);

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
            clientBuilder.AddStandardResilienceHandler(o => 
            {
                o.AttemptTimeout.OnTimeout = (args) =>
                {
                    var uri = args.Context.GetRequestMessage()?.RequestUri ?? new Uri("unknown://uri");
                    outputHelper.WriteLine($"Attempt timeout after {args.Timeout} for request to {uri}");
                    return ValueTask.CompletedTask;
                };

                o.TotalRequestTimeout.OnTimeout = (args) =>
                {
                    var uri = args.Context.GetRequestMessage()?.RequestUri ?? new Uri("unknown://uri");
                    outputHelper.WriteLine($"Total request timeout after {args.Timeout} for request to {uri}");
                    return ValueTask.CompletedTask;
                };
            });
        });

        App = await appHost
            .BuildAsync(Cancel)
            .WaitAsync(builderStepTimeout, Cancel);

        await App
            .StartAsync(Cancel)
            .WaitAsync(builderStepTimeout, Cancel);
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (App is not null)
        {
            await App.DisposeAsync();
        }

        if (_sqlServer is not null)
        {
            await _sqlServer.DisconnectAsync();
        }
    }
}

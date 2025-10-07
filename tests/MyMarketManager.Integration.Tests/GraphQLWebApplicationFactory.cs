using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyMarketManager.WebApp;

namespace MyMarketManager.Integration.Tests;

/// <summary>
/// Custom WebApplicationFactory for integration testing with SQLite
/// </summary>
public class GraphQLWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Configure to use SQLite for testing
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override configuration to use SQLite
            context.HostingEnvironment.EnvironmentName = "Testing";
        });

        return base.CreateHost(builder);
    }
}

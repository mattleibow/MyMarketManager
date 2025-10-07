using Microsoft.Extensions.DependencyInjection;

namespace MyMarketManager.GraphQL.Client;

/// <summary>
/// Extension methods for registering the GraphQL client in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the MyMarketManager GraphQL client to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureClient">Optional action to configure the HttpClient.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMyMarketManagerGraphQLClient(
        this IServiceCollection services,
        Action<IServiceProvider, HttpClient>? configureClient = null)
    {
        // The generated client will be added here by StrawberryShake
        // For now, we'll configure the HttpClient factory
        if (configureClient != null)
        {
            services.AddHttpClient("MyMarketManagerClient", configureClient);
        }
        else
        {
            services.AddHttpClient("MyMarketManagerClient");
        }

        return services;
    }

    /// <summary>
    /// Adds the MyMarketManager GraphQL client to the service collection with a base URL.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseUrl">The base URL of the GraphQL endpoint.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMyMarketManagerGraphQLClient(
        this IServiceCollection services,
        string baseUrl)
    {
        return services.AddMyMarketManagerGraphQLClient((sp, client) =>
        {
            client.BaseAddress = new Uri(baseUrl);
        });
    }
}

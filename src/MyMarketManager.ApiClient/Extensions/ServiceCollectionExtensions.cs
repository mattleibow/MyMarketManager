using Microsoft.Extensions.DependencyInjection;

namespace MyMarketManager.ApiClient.Extensions;

/// <summary>
/// Extension methods for registering API client services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add ProductsClient with HttpClient configured to use the local API
    /// </summary>
    public static IServiceCollection AddProductsClient(this IServiceCollection services, Action<HttpClient>? configureClient = null)
    {
        services.AddHttpClient<ProductsClient>(client =>
        {
            // Default configuration - will be called by the caller if they want to customize
            configureClient?.Invoke(client);
        });

        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MyMarketManager.Data;

/// <summary>
/// Custom model cache key factory that creates different models for different database providers.
/// This is necessary because SQLite doesn't support Vector types while PostgreSQL does.
/// </summary>
public class MyMarketManagerModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        // Include the provider name in the cache key so different providers get different models
        return (context.GetType(), context.Database.ProviderName, designTime);
    }
}

using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MyMarketManager.GraphQL.Server;

/// <summary>
/// Extension methods for registering GraphQL server services
/// </summary>
public static class GraphQLServerExtensions
{
    /// <summary>
    /// Adds the MyMarketManager GraphQL server to the service collection
    /// </summary>
    public static IRequestExecutorBuilder AddMyMarketManagerGraphQLServer(this IServiceCollection services)
    {
        return services
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query"))
                .AddTypeExtension<ProductQueries>()
                .AddTypeExtension<PurchaseOrderQueries>()
                .AddTypeExtension<PurchaseOrderIngestionQueries>()
                .AddTypeExtension<StagingBatchQueries>()
                .AddTypeExtension<StagingPurchaseOrderQueries>()
                .AddTypeExtension<SupplierQueries>()
            .AddMutationType(d => d.Name("Mutation"))
                .AddTypeExtension<ProductMutations>()
                .AddTypeExtension<PurchaseOrderIngestionMutations>()
                .AddTypeExtension<StagingPurchaseOrderMutations>()
            .AddProjections()
            .AddFiltering()
            .AddSorting();
    }
}

using HotChocolate;
using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data;

namespace MyMarketManager.WebApp.GraphQL;

/// <summary>
/// GraphQL queries for suppliers
/// </summary>
[ExtendObjectType("Query")]
public class SupplierQueries
{
    /// <summary>
    /// Get all suppliers for dropdown
    /// </summary>
    public async Task<List<SupplierOption>> GetSuppliers(
        MyMarketManagerDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Suppliers
            .OrderBy(s => s.Name)
            .Select(s => new SupplierOption(s.Id, s.Name))
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Supplier option for dropdown
/// </summary>
public record SupplierOption(Guid Id, string Name);
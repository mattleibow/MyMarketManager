using MyMarketManager.Data.Entities;
using MyMarketManager.Tests.Shared;

namespace MyMarketManager.GraphQL.Server.Tests;

[Trait(TestCategories.Key, TestCategories.Values.GraphQL)]
public class SupplierQueriesTests(ITestOutputHelper outputHelper) : GraphQLTestBase(outputHelper, createSchema: true)
{
    [Fact]
    public async Task GetSuppliers_ShouldReturnAllSuppliers_OrderedByName()
    {
        // Arrange
        var supplier1 = new Supplier { Id = Guid.NewGuid(), Name = "Zebra Supplies" };
        var supplier2 = new Supplier { Id = Guid.NewGuid(), Name = "Apple Wholesale" };
        var supplier3 = new Supplier { Id = Guid.NewGuid(), Name = "Best Products" };

        Context.Suppliers.AddRange(supplier1, supplier2, supplier3);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<SuppliersResponse>(@"
            query {
                suppliers(order: { name: ASC }) {
                    id
                    name
                }
            }
        ");

        // Assert
        Assert.Equal(3, result.Suppliers.Count);
        Assert.Equal("Apple Wholesale", result.Suppliers[0].Name);
        Assert.Equal("Best Products", result.Suppliers[1].Name);
        Assert.Equal("Zebra Supplies", result.Suppliers[2].Name);
    }

    [Fact]
    public async Task GetSuppliers_ShouldNotIncludeDeletedSuppliers()
    {
        // Arrange
        var supplier1 = new Supplier { Id = Guid.NewGuid(), Name = "Active Supplier" };
        var supplier2 = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Deleted Supplier",
            DeletedAt = DateTimeOffset.UtcNow
        };

        Context.Suppliers.AddRange(supplier1, supplier2);
        await Context.SaveChangesAsync(Cancel);

        // Act
        var result = await ExecuteQueryAsync<SuppliersResponse>(@"
            query {
                suppliers {
                    id
                    name
                }
            }
        ");

        // Assert
        Assert.Single(result.Suppliers);
        Assert.Equal("Active Supplier", result.Suppliers[0].Name);
    }

    [Fact]
    public async Task GetSuppliers_WithNoSuppliers_ShouldReturnEmptyList()
    {
        // Act
        var result = await ExecuteQueryAsync<SuppliersResponse>(@"
            query {
                suppliers {
                    id
                    name
                }
            }
        ");

        // Assert
        Assert.Empty(result.Suppliers);
    }

    private record SuppliersResponse(List<SupplierDto> Suppliers);
    private record SupplierDto(Guid Id, string Name);
}

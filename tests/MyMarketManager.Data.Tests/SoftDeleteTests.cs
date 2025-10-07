using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Tests;

public class SoftDeleteTests : SqliteTestBase
{
    [Fact]
    public async Task Entity_WhenDeleted_ShouldBeSoftDeleted()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Test Supplier"
        };
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        Context.Suppliers.Remove(supplier);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - entity should not be returned by default queries
        var foundSupplier = await Context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplier.Id, TestContext.Current.CancellationToken);
        Assert.Null(foundSupplier);

        // But should be found when ignoring query filters
        var deletedSupplier = await Context.Suppliers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == supplier.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(deletedSupplier);
        Assert.NotNull(deletedSupplier.DeletedAt);
        Assert.True(deletedSupplier.IsDeleted);
    }

    [Fact]
    public async Task Entity_WhenCreated_ShouldHaveAuditTimestamps()
    {
        // Arrange
        var beforeCreate = DateTimeOffset.UtcNow.AddSeconds(-1);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Quality = ProductQuality.Good
        };

        // Act
        Context.Products.Add(product);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var afterCreate = DateTimeOffset.UtcNow.AddSeconds(1);

        // Assert
        Assert.True(product.CreatedAt >= beforeCreate && product.CreatedAt <= afterCreate);
        Assert.True(product.UpdatedAt >= beforeCreate && product.UpdatedAt <= afterCreate);
        Assert.Null(product.DeletedAt);
        Assert.False(product.IsDeleted);
    }

    [Fact]
    public async Task Entity_WhenModified_ShouldUpdateTimestamp()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            Quality = ProductQuality.Good
        };
        Context.Products.Add(product);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var originalCreatedAt = product.CreatedAt;
        var originalUpdatedAt = product.UpdatedAt;

        // Wait a bit to ensure timestamp difference
        await Task.Delay(10, TestContext.Current.CancellationToken);

        // Act
        product.Name = "Updated Name";
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(originalCreatedAt, product.CreatedAt); // CreatedAt should not change
        Assert.True(product.UpdatedAt > originalUpdatedAt); // UpdatedAt should be updated
        Assert.Null(product.DeletedAt);
    }

    [Fact]
    public async Task QueryFilter_ShouldExcludeSoftDeletedEntities()
    {
        // Arrange
        var supplier1 = new Supplier { Id = Guid.NewGuid(), Name = "Active Supplier" };
        var supplier2 = new Supplier { Id = Guid.NewGuid(), Name = "Deleted Supplier" };

        Context.Suppliers.AddRange(supplier1, supplier2);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Delete one supplier
        Context.Suppliers.Remove(supplier2);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var activeSuppliers = await Context.Suppliers.ToListAsync(TestContext.Current.CancellationToken);
        var allSuppliers = await Context.Suppliers.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(activeSuppliers);
        Assert.Equal("Active Supplier", activeSuppliers[0].Name);

        Assert.Equal(2, allSuppliers.Count);
    }
}

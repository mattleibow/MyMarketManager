using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data.Services;
using MyMarketManager.Tests.Shared;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MyMarketManager.Data.Tests.Services;

[Trait(TestCategories.Key, TestCategories.Values.Database)]
public class DbContextMigratorTests(ITestOutputHelper outputHelper, bool createSchema = false) : PostgresTestBase(outputHelper, createSchema)
{
    private readonly ILogger<DbContextMigrator> _logger = outputHelper.ToLogger<DbContextMigrator>();
    private readonly IHostEnvironment _mockEnvironment = Substitute.For<IHostEnvironment>();

    [Fact]
    public async Task InDevelopment_ShouldSeedData()
    {
        // Arrange
        _mockEnvironment.EnvironmentName.Returns(Environments.Development);
        var migrator = new DbContextMigrator(Context, _mockEnvironment, _logger);

        // Ensure database tables don't exist yet
        Assert.False(await TableExistsAsync("Products"));
        Assert.False(await TableExistsAsync("Suppliers"));

        // Act
        await migrator.MigrateAsync(Cancel);

        // Assert - verify data was seeded
        var suppliers = await Context.Suppliers.ToListAsync(Cancel);
        var products = await Context.Products.ToListAsync(Cancel);

        Assert.True(suppliers.Count > 0, "Suppliers should be seeded in development");
        Assert.True(products.Count > 0, "Products should be seeded in development");
    }

    [Fact]
    public async Task InProduction_ShouldNotSeedData()
    {
        // Arrange
        _mockEnvironment.EnvironmentName.Returns(Environments.Production);
        var migrator = new DbContextMigrator(Context, _mockEnvironment, _logger);

        // Ensure database tables don't exist yet
        Assert.False(await TableExistsAsync("Products"));
        Assert.False(await TableExistsAsync("Suppliers"));

        // Act
        await migrator.MigrateAsync(Cancel);

        // Verify no data was seeded
        Assert.False(await Context.Products.AnyAsync(Cancel));
        Assert.False(await Context.Suppliers.AnyAsync(Cancel));
    }

    [Fact]
    public async Task WhenExceptionOccurs_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var mockContext = Substitute.For<MyMarketManagerDbContext>(new DbContextOptionsBuilder<MyMarketManagerDbContext>().Options);
        var mockDatabase = Substitute.For<Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade>(mockContext);

        mockContext.Database.Returns(mockDatabase);
        mockDatabase.CreateExecutionStrategy().Throws(new InvalidOperationException("Test exception"));

        var migrator = new DbContextMigrator(mockContext, _mockEnvironment, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => migrator.MigrateAsync(Cancel));
    }
}

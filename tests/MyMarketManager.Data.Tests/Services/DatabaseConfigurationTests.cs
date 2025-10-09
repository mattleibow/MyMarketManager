using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data.Services;
using NSubstitute;

namespace MyMarketManager.Data.Tests.Services;

public class DatabaseConfigurationTests(ITestOutputHelper outputHelper)
{
    private readonly ILogger<MyMarketManagerDbContextMigrator> _logger = outputHelper.ToLogger<MyMarketManagerDbContextMigrator>();
    private readonly IHostEnvironment _mockEnvironment = Substitute.For<IHostEnvironment>();

    [Fact]
    public async Task WhenUseSqliteIsTrue_ShouldUseEnsureCreated()
    {
        // Arrange
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseSqliteDatabase"] = "True"
            })
            .Build();

        // Use a file-based SQLite database for this test
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<MyMarketManagerDbContext>()
            .UseSqlite($"DataSource={dbPath}")
            .Options;

        using var context = new MyMarketManagerDbContext(options);
        
        _mockEnvironment.EnvironmentName.Returns(Environments.Development);
        var migrator = new MyMarketManagerDbContextMigrator(context, _mockEnvironment, config, _logger);

        try
        {
            // Act - should not throw because EnsureCreated is used instead of Migrate
            await migrator.MigrateAsync();

            // Assert - verify the database was created
            var canConnect = await context.Database.CanConnectAsync();
            Assert.True(canConnect, "Database should be created using EnsureCreated");
            
            // Verify products table exists and has seeded data
            var productCount = await context.Products.CountAsync();
            Assert.True(productCount > 0, "Products should be seeded in development");
        }
        finally
        {
            // Cleanup
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public async Task WhenUseSqliteIsFalse_ShouldUseMigrate()
    {
        // Arrange
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseSqliteDatabase"] = "False"
            })
            .Build();

        var options = new DbContextOptionsBuilder<MyMarketManagerDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        using var context = new MyMarketManagerDbContext(options);
        
        _mockEnvironment.EnvironmentName.Returns(Environments.Development);
        var migrator = new MyMarketManagerDbContextMigrator(context, _mockEnvironment, config, _logger);

        // Act & Assert - should throw because Migrate requires migrations
        // SQLite doesn't support migrations like SQL Server, so this will fail
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await migrator.MigrateAsync());
    }

    [Fact]
    public async Task WhenUseSqliteIsNotSet_ShouldDefaultToFalse()
    {
        // Arrange
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var options = new DbContextOptionsBuilder<MyMarketManagerDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        using var context = new MyMarketManagerDbContext(options);
        
        _mockEnvironment.EnvironmentName.Returns(Environments.Development);
        var migrator = new MyMarketManagerDbContextMigrator(context, _mockEnvironment, config, _logger);

        // Act & Assert - should throw because default is false (use Migrate)
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await migrator.MigrateAsync());
    }
}

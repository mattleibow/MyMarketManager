using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyMarketManager.Data.Entities;
using MyMarketManager.Data.Enums;

namespace MyMarketManager.Data.Services;

/// <summary>
/// Service responsible for database migration and seeding operations.
/// </summary>
public class DbContextMigrator(
    MyMarketManagerDbContext context,
    IHostEnvironment environment,
    ILogger<DbContextMigrator> logger)
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Starting database migration...");

            // Apply migrations using execution strategy for resilience
            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await context.Database.MigrateAsync(cancellationToken);
            });

            logger.LogInformation("Database migrations applied successfully.");

            // Only seed sample data in development/testing environment
            if (!environment.IsProduction())
            {
                if (!await context.Products.AnyAsync(cancellationToken))
                {
                    await SeedInitialDataAsync(context, cancellationToken);
                    logger.LogInformation("Database seeded with initial data.");
                }
                else
                {
                    logger.LogInformation("Database already contains data, skipping seeding.");
                }
            }
            else
            {
                logger.LogInformation("Production environment - skipping sample data seeding.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database.");
            throw;
        }
    }

    /// <summary>
    /// Seeds the database with initial sample data for development purposes.
    /// </summary>
    private static async Task SeedInitialDataAsync(MyMarketManagerDbContext context, CancellationToken cancellationToken)
    {
        // Add some sample suppliers
        var suppliers = new[]
        {
            new Supplier
            {
                Name = "Fashion Wholesale Co.",
                WebsiteUrl = "https://fashionwholesale.com",
                ContactInfo = "orders@fashionwholesale.com"
            },
            new Supplier
            {
                Name = "Electronics Direct",
                WebsiteUrl = "https://electronicsdirect.com",
                ContactInfo = "+1-555-0123"
            },
            new Supplier
            {
                Name = "Home & Garden Supply",
                ContactInfo = "info@homegardensupp.ly"
            }
        };

        // Use execution strategy for seeding as well to handle transient errors
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            context.Suppliers.AddRange(suppliers);
            await context.SaveChangesAsync(cancellationToken);

            // Add some sample products
            var products = new[]
            {
                new Product
                {
                    SKU = "TSHIRT-001",
                    Name = "Cotton T-Shirt",
                    Description = "100% cotton, comfortable fit, multiple colors available",
                    Quality = ProductQuality.Good,
                    StockOnHand = 25,
                    Notes = "Popular item, restock frequently",
                    Photos =
                    [
                        new ProductPhoto { Url = "https://raw.githubusercontent.com/mattleibow/MyMarketManager/refs/heads/main/assets/seeding/TSHIRT-001-Black.png", MimeType = "image/png" },
                        new ProductPhoto { Url = "https://raw.githubusercontent.com/mattleibow/MyMarketManager/refs/heads/main/assets/seeding/TSHIRT-001-Blue.png", MimeType = "image/png" }
                    ]
                },
                new Product
                {
                    SKU = "JEANS-002",
                    Name = "Denim Jeans",
                    Description = "Classic blue denim jeans, various sizes",
                    Quality = ProductQuality.Excellent,
                    StockOnHand = 15,
                    Notes = "Premium quality denim",
                    Photos =
                    [
                        new ProductPhoto { Url = "https://raw.githubusercontent.com/mattleibow/MyMarketManager/refs/heads/main/assets/seeding/JEANS-002.png", MimeType = "image/png" }
                    ]
                },
                new Product
                {
                    Name = "Wireless Earbuds",
                    Description = "Bluetooth wireless earbuds with charging case",
                    Quality = ProductQuality.Good,
                    StockOnHand = 8,
                    Notes = "Check battery life before selling",
                    Photos =
                    [
                        new ProductPhoto { Url = "https://raw.githubusercontent.com/mattleibow/MyMarketManager/refs/heads/main/assets/seeding/Earbuds.png", MimeType = "image/png" }
                    ]
                },
                new Product
                {
                    SKU = "PLANT-004",
                    Name = "Succulent Plant",
                    Description = "Low-maintenance succulent in decorative pot",
                    Quality = ProductQuality.Fair,
                    StockOnHand = 12,
                    Notes = "Handle with care during transport",
                    Photos =
                    [
                        new ProductPhoto { Url = "https://raw.githubusercontent.com/mattleibow/MyMarketManager/refs/heads/main/assets/seeding/PLANT-004.png", MimeType = "image/png" }
                    ]
                },
                new Product
                {
                    SKU = "MUG-005",
                    Name = "Ceramic Coffee Mug",
                    Description = "Handmade ceramic mug with unique glaze",
                    Quality = ProductQuality.Excellent,
                    StockOnHand = 0,
                    Notes = "Out of stock - very popular item"
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }
}

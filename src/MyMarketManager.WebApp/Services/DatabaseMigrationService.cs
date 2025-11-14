using MyMarketManager.Data.Services;

namespace MyMarketManager.WebApp.Services;

/// <summary>
/// Background service that handles database migration during application startup.
/// </summary>
public class DatabaseMigrationService(
    IServiceProvider serviceProvider,
    ILogger<DatabaseMigrationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var migrator = scope.ServiceProvider.GetRequiredService<DbContextMigrator>();

            logger.LogInformation("Starting database migration service...");

            await migrator.MigrateAsync(stoppingToken);

            logger.LogInformation("Database migration service completed.");

            // Keep the service alive until the application is shutting down
            // This prevents the app from exiting after migration completes
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when application is shutting down
            logger.LogInformation("Database migration service shutting down.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error in database migration service.");
            throw;
        }
    }
}

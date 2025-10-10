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
        using var scope = serviceProvider.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<DbContextMigrator>();

        logger.LogInformation("Starting database migration service...");

        await migrator.MigrateAsync(stoppingToken);

        logger.LogInformation("Database migration service completed.");
    }
}

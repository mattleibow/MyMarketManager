using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.Data;

/// <summary>
/// Database context for MyMarketManager application.
/// </summary>
public class MyMarketManagerDbContext : DbContext
{
    // Static value converters for DateTimeOffset to DateTime conversion (SQLite compatibility)
    private static readonly ValueConverter<DateTimeOffset, DateTime> DateTimeOffsetToDateTimeConverter =
        new(v => v.UtcDateTime, v => new DateTimeOffset(v, TimeSpan.Zero));

    private static readonly ValueConverter<DateTimeOffset?, DateTime?> NullableDateTimeOffsetToDateTimeConverter =
        new(v => v.HasValue ? v.Value.UtcDateTime : null, v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null);

    public MyMarketManagerDbContext(DbContextOptions<MyMarketManagerDbContext> options)
        : base(options)
    {
    }

    // Core entities
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryItem> DeliveryItems => Set<DeliveryItem>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductPhoto> ProductPhotos => Set<ProductPhoto>();
    public DbSet<MarketEvent> MarketEvents => Set<MarketEvent>();
    public DbSet<ReconciledSale> ReconciledSales => Set<ReconciledSale>();

    // Staging entities
    public DbSet<StagingBatch> StagingBatches => Set<StagingBatch>();
    public DbSet<StagingPurchaseOrder> StagingPurchaseOrders => Set<StagingPurchaseOrder>();
    public DbSet<StagingPurchaseOrderItem> StagingPurchaseOrderItems => Set<StagingPurchaseOrderItem>();
    public DbSet<StagingSale> StagingSales => Set<StagingSale>();
    public DbSet<StagingSaleItem> StagingSaleItems => Set<StagingSaleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var entity = modelBuilder.Entity(clrType);

            // Configure decimal precision for all decimal properties
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(decimal) ||
                    property.ClrType == typeof(decimal?))
                {
                    entity.Property(property.Name).HasPrecision(18, 4);
                }

                // Configure DateTimeOffset to DateTime conversion for SQLite compatibility
                // SQLite stores DateTimeOffset as TEXT, but we can store as DateTime in UTC instead
                if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
                {
                    if (property.ClrType == typeof(DateTimeOffset))
                    {
                        entity.Property(property.Name)
                            .HasConversion(DateTimeOffsetToDateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTimeOffset?))
                    {
                        entity.Property(property.Name)
                            .HasConversion(NullableDateTimeOffsetToDateTimeConverter);
                    }
                }
            }

            // Configure soft delete query filters for all entities
            if (typeof(IAuditable).IsAssignableFrom(clrType))
            {
                // Add query filter to exclude soft-deleted entities
                var parameter = System.Linq.Expressions.Expression.Parameter(clrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(IAuditable.DeletedAt));
                var nullCheck = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(null, typeof(DateTimeOffset?)));
                var lambda = System.Linq.Expressions.Expression.Lambda(nullCheck, parameter);

                entity.HasQueryFilter(lambda);
            }
        }
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();

        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();

        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<IAuditable>();
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;

                case EntityState.Deleted:
                    // Soft delete: mark as deleted instead of actually deleting
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.Data;

/// <summary>
/// Database context for MyMarketManager application.
/// </summary>
public class MyMarketManagerDbContext : DbContext
{
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

        // Configure soft delete query filters for all entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IAuditable).IsAssignableFrom(entityType.ClrType))
            {
                // Add query filter to exclude soft-deleted entities
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(IAuditable.DeletedAt));
                var nullCheck = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(null, typeof(DateTimeOffset?)));
                var lambda = System.Linq.Expressions.Expression.Lambda(nullCheck, parameter);
                
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
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

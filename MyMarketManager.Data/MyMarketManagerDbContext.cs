using Microsoft.EntityFrameworkCore;
using MyMarketManager.Data.Entities;

namespace MyMarketManager.Data;

/// <summary>
/// Database context for MyMarketManager application.
/// </summary>
public class MyMarketManagerDbContext : DbContext
{
    public MyMarketManagerDbContext()
    {
    }

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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // For design-time only (migrations). Override with actual connection in startup.
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MyMarketManager;Trusted_Connection=True;MultipleActiveResultSets=true");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure delete behaviors for relationships where needed
        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(e => e.Supplier)
            .WithMany(s => s.PurchaseOrders)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrderItem>()
            .HasOne(e => e.Product)
            .WithMany(p => p.PurchaseOrderItems)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Delivery>()
            .HasOne(e => e.PurchaseOrder)
            .WithMany(p => p.Deliveries)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DeliveryItem>()
            .HasOne(e => e.Product)
            .WithMany(p => p.DeliveryItems)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReconciledSale>()
            .HasOne(e => e.Product)
            .WithMany(p => p.ReconciledSales)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StagingBatch>()
            .HasOne(e => e.Supplier)
            .WithMany(s => s.StagingBatches)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StagingPurchaseOrder>()
            .HasOne(e => e.PurchaseOrder)
            .WithMany(p => p.StagingPurchaseOrders)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StagingPurchaseOrderItem>()
            .HasOne(e => e.Product)
            .WithMany(p => p.StagingPurchaseOrderItems)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StagingPurchaseOrderItem>()
            .HasOne(e => e.Supplier)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StagingPurchaseOrderItem>()
            .HasOne(e => e.PurchaseOrderItem)
            .WithMany(p => p.StagingPurchaseOrderItems)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StagingSaleItem>()
            .HasOne(e => e.Product)
            .WithMany(p => p.StagingSaleItems)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

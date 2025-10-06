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

        // Supplier
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.WebsiteUrl).HasMaxLength(500);
            entity.Property(e => e.ContactInfo).HasMaxLength(500);
        });

        // PurchaseOrder
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShippingFees).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ImportFees).HasColumnType("decimal(18,2)");
            entity.Property(e => e.InsuranceFees).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AdditionalFees).HasColumnType("decimal(18,2)");
            
            entity.HasOne(e => e.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PurchaseOrderItem
        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SupplierReference).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SupplierProductUrl).HasMaxLength(1000);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ListedUnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ActualUnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AllocatedUnitOverhead).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalUnitCost).HasColumnType("decimal(18,2)");
            
            entity.HasOne(e => e.PurchaseOrder)
                .WithMany(p => p.Items)
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Product)
                .WithMany(p => p.PurchaseOrderItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SKU).HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasIndex(e => e.SKU).IsUnique();
        });

        // ProductPhoto
        modelBuilder.Entity<ProductPhoto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Caption).HasMaxLength(500);
            
            entity.HasOne(e => e.Product)
                .WithMany(p => p.Photos)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Delivery
        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Courier).HasMaxLength(200);
            entity.Property(e => e.TrackingNumber).HasMaxLength(200);
            
            entity.HasOne(e => e.PurchaseOrder)
                .WithMany(p => p.Deliveries)
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // DeliveryItem
        modelBuilder.Entity<DeliveryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Delivery)
                .WithMany(d => d.Items)
                .HasForeignKey(e => e.DeliveryId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Product)
                .WithMany(p => p.DeliveryItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MarketEvent
        modelBuilder.Entity<MarketEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Location).HasMaxLength(300);
        });

        // ReconciledSale
        modelBuilder.Entity<ReconciledSale>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SalePrice).HasColumnType("decimal(18,2)");
            
            entity.HasOne(e => e.Product)
                .WithMany(p => p.ReconciledSales)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.MarketEvent)
                .WithMany(m => m.ReconciledSales)
                .HasForeignKey(e => e.MarketEventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StagingBatch
        modelBuilder.Entity<StagingBatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileHash).IsRequired().HasMaxLength(100);
            
            entity.HasOne(e => e.Supplier)
                .WithMany(s => s.StagingBatches)
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // StagingPurchaseOrder
        modelBuilder.Entity<StagingPurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SupplierReference).IsRequired().HasMaxLength(200);
            entity.Property(e => e.RawData).IsRequired();
            
            entity.HasOne(e => e.StagingBatch)
                .WithMany(b => b.StagingPurchaseOrders)
                .HasForeignKey(e => e.StagingBatchId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.PurchaseOrder)
                .WithMany(p => p.StagingPurchaseOrders)
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // StagingPurchaseOrderItem
        modelBuilder.Entity<StagingPurchaseOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SupplierReference).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SupplierProductUrl).HasMaxLength(1000);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ListedUnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ActualUnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RawData).IsRequired();
            
            entity.HasOne(e => e.StagingPurchaseOrder)
                .WithMany(o => o.Items)
                .HasForeignKey(e => e.StagingPurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Product)
                .WithMany(p => p.StagingPurchaseOrderItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Supplier)
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.PurchaseOrderItem)
                .WithMany(p => p.StagingPurchaseOrderItems)
                .HasForeignKey(e => e.PurchaseOrderItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // StagingSale
        modelBuilder.Entity<StagingSale>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RawData).IsRequired();
            
            entity.HasOne(e => e.StagingBatch)
                .WithMany(b => b.StagingSales)
                .HasForeignKey(e => e.StagingBatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StagingSaleItem
        modelBuilder.Entity<StagingSaleItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductDescription).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MarketEventName).HasMaxLength(200);
            entity.Property(e => e.RawData).IsRequired();
            
            entity.HasOne(e => e.StagingSale)
                .WithMany(s => s.Items)
                .HasForeignKey(e => e.StagingSaleId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Product)
                .WithMany(p => p.StagingSaleItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

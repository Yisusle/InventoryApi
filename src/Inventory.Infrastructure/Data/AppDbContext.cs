using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Entities;

namespace Inventory.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleLine> SaleLines => Set<SaleLine>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.Username).IsRequired().HasMaxLength(100).IsUnicode();
            b.Property(u => u.Email).IsRequired().HasMaxLength(200).IsUnicode();
            b.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            b.Property(u => u.Role).IsRequired().HasMaxLength(50).HasDefaultValue("User");
            b.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(u => u.Username).IsUnique();
            b.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Category>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Name).IsRequired().HasMaxLength(100).IsUnicode();
            b.Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200).IsUnicode();
            b.Property(p => p.Sku).HasMaxLength(100);
            b.Property(p => p.Price).HasColumnType("decimal(18,2)").IsRequired();
            b.Property(p => p.Stock).HasDefaultValue(0).IsRequired();
            b.Property(p => p.MinimumStock).HasDefaultValue(0).IsRequired();
            b.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            b.Property(p => p.RowVersion).IsRowVersion();
            b.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(p => p.Sku).IsUnique().HasFilter("[Sku] IS NOT NULL");
            b.HasIndex(p => p.Name);
            b.HasIndex(p => p.CategoryId);
        });

        modelBuilder.Entity<Purchase>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Date).HasDefaultValueSql("GETUTCDATE()").IsRequired();
            b.Property(p => p.ProductId).IsRequired();
            b.Property(p => p.Quantity).IsRequired();
            b.Property(p => p.TotalCost).HasColumnType("decimal(18,2)").IsRequired();
            b.Property(p => p.CreatedByUserId).IsRequired();
            b.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasOne(p => p.Product).WithMany().HasForeignKey(p => p.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(p => p.CreatedByUser).WithMany().HasForeignKey(p => p.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(p => p.ProductId);
            b.HasIndex(p => p.CreatedByUserId);
            b.HasIndex(p => p.Date);
        });

        modelBuilder.Entity<Sale>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Date).HasDefaultValueSql("GETUTCDATE()").IsRequired();
            b.Property(s => s.CreatedByUserId).IsRequired();
            b.Ignore(s => s.Total);
            b.Ignore(s => s.TotalItems);
            b.Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasOne(s => s.CreatedByUser).WithMany().HasForeignKey(s => s.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(s => s.Lines).WithOne(line => line.Sale).HasForeignKey(line => line.SaleId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(s => s.CreatedByUserId);
            b.HasIndex(s => s.Date);
        });

        modelBuilder.Entity<SaleLine>(b =>
        {
            b.HasKey(line => line.Id);
            b.Property(line => line.ProductName).IsRequired().HasMaxLength(200).IsUnicode();
            b.Property(line => line.Quantity).IsRequired();
            b.Property(line => line.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
            b.Ignore(line => line.Total);
            b.HasOne(line => line.Product).WithMany().HasForeignKey(line => line.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(line => line.SaleId);
            b.HasIndex(line => line.ProductId);
        });

        modelBuilder.Entity<InventoryMovement>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.Type).IsRequired().HasMaxLength(20);
            b.Property(m => m.Reason).IsRequired().HasMaxLength(500).IsUnicode();
            b.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasOne(m => m.Product).WithMany().HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(m => m.Sale).WithMany().HasForeignKey(m => m.SaleId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(m => m.Purchase).WithMany().HasForeignKey(m => m.PurchaseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(m => m.PerformedByUser).WithMany().HasForeignKey(m => m.PerformedByUserId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(m => m.ProductId);
            b.HasIndex(m => m.CreatedAt);
        });
    }
}

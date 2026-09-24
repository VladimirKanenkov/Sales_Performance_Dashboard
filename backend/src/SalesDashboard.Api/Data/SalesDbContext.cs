using Microsoft.EntityFrameworkCore;
using SalesDashboard.Api.Domain;

namespace SalesDashboard.Api.Data;

/// <summary>
/// Контекст EF Core для справочников и продаж. Индексы по дате и менеджеру
/// ускоряют выборку аналитики за период.
/// </summary>
public class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    /// <summary>Менеджеры.</summary>
    public DbSet<Manager> Managers => Set<Manager>();

    /// <summary>Клиенты.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Категории товаров.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Товары.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Продажи.</summary>
    public DbSet<Sale> Sales => Set<Sale>();

    /// <summary>Позиции продаж.</summary>
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Manager>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            e.Property(x => x.Team).HasMaxLength(80).IsRequired();
            e.Property(x => x.Title).HasMaxLength(80).IsRequired();
            e.Property(x => x.Initials).HasMaxLength(4).IsRequired();
            e.Property(x => x.AvatarColor).HasMaxLength(16).IsRequired();
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Company).HasMaxLength(160).IsRequired();
            e.Property(x => x.Segment).HasMaxLength(40).IsRequired();
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.Sku).HasMaxLength(40).IsRequired();
            e.Property(x => x.Brand).HasMaxLength(80).IsRequired();
            e.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId);
            e.HasIndex(x => x.CategoryId);
        });

        modelBuilder.Entity<Sale>(e =>
        {
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.SaleDate).HasColumnType("date");
            e.HasOne(x => x.Manager).WithMany(x => x.Sales).HasForeignKey(x => x.ManagerId);
            e.HasOne(x => x.Customer).WithMany(x => x.Sales).HasForeignKey(x => x.CustomerId);
            e.HasIndex(x => x.SaleDate);
            e.HasIndex(x => new { x.ManagerId, x.SaleDate });
            e.HasIndex(x => new { x.Status, x.SaleDate });
        });

        modelBuilder.Entity<SaleItem>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.UnitCost).HasPrecision(18, 2);
            e.HasOne(x => x.Sale).WithMany(x => x.Items).HasForeignKey(x => x.SaleId);
            e.HasOne(x => x.Product).WithMany(x => x.SaleItems).HasForeignKey(x => x.ProductId);
            e.HasIndex(x => x.SaleId);
            e.HasIndex(x => x.ProductId);
        });
    }
}

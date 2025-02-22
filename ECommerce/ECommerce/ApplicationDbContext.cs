using ECommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Cart { get; set; } // Sepet Tablosu
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Log> Logs { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Customer Ayarları
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(c => c.CustomerID);
                entity.Property(c => c.CustomerName).IsRequired().HasMaxLength(100);
                entity.Property(c => c.CustomerSurName).IsRequired().HasMaxLength(100);
                entity.Property(c => c.CustomerLoginName).IsRequired().HasMaxLength(50);
                entity.Property(c => c.CustomerPassword).IsRequired().HasMaxLength(50);
                entity.Property(c => c.Budget).HasColumnType("float"); // double türü
                entity.Property(c => c.CustomerType).IsRequired().HasMaxLength(10); // Premium / Standard
                entity.Property(c => c.TotalSpent).HasColumnType("float");
            });

            // Category Ayarları
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.CategoryID);
                entity.Property(c => c.CategoryName).IsRequired().HasMaxLength(100);

                entity.HasOne(c => c.ParentCategory)
                      .WithMany(c => c.SubCategories)
                      .HasForeignKey(c => c.ParentCategoryID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Product Ayarları
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.ProductID);
                entity.Property(p => p.ProductName).IsRequired().HasMaxLength(200);
                entity.Property(p => p.Stock).IsRequired();
                entity.Property(p => p.Price).IsRequired().HasColumnType("float");
                entity.Property(p => p.Description).HasMaxLength(500);

                entity.HasOne(p => p.Category)
                      .WithMany(c => c.Products)
                      .HasForeignKey(p => p.CategoryID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Cart Ayarları
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasKey(c => c.CartID);
                entity.HasOne(c => c.Customer)
                      .WithMany()
                      .HasForeignKey(c => c.CustomerID);
                entity.HasOne(c => c.Product)
                      .WithMany()
                      .HasForeignKey(c => c.ProductID);
            });
        }
    }
}

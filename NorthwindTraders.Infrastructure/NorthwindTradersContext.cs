using Microsoft.EntityFrameworkCore;
using NorthwindTraders.Application.Common;
using NorthwindTraders.Domain.Models;
using NorthwindTraders.Domain.Models.Identity;

namespace NorthwindTraders.Infrastructure
{
    public class NorthwindTradersContext : Microsoft.EntityFrameworkCore.DbContext, INorthwindDbContext
    {
        public NorthwindTradersContext(Microsoft.EntityFrameworkCore.DbContextOptions<NorthwindTradersContext> options)
            : base(options)
        {
        }

        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 👇 Tell EF the exact table names in SQL Server
            modelBuilder.Entity<Customer>().ToTable("Customer");
            modelBuilder.Entity<Supplier>().ToTable("Supplier");
            modelBuilder.Entity<Product>().ToTable("Product");
            modelBuilder.Entity<Order>().ToTable("Order");
            modelBuilder.Entity<OrderItem>().ToTable("OrderItem");

            // Your relationship config (if you added it) can stay here too
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId);
            modelBuilder.Entity<UserRole>()
            .HasKey(x => new { x.UserId, x.RoleId });

        }
    }
}

using Microsoft.EntityFrameworkCore;
using NorthwindTraders.Domain.Models;

namespace NorthwindTraders.Application.Common;

public interface INorthwindDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<Product> Products { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Supplier> Suppliers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
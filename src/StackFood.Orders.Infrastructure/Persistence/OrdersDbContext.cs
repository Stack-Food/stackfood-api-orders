using Microsoft.EntityFrameworkCore;
using StackFood.Orders.Domain.Entities;
using System.Diagnostics.CodeAnalysis;
namespace StackFood.Orders.Infrastructure.Persistence;

[ExcludeFromCodeCoverage]
public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

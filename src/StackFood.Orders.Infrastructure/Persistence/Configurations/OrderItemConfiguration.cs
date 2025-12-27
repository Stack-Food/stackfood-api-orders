using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackFood.Orders.Domain.Entities;
using System.Diagnostics.CodeAnalysis;
namespace StackFood.Orders.Infrastructure.Persistence.Configurations;

[ExcludeFromCodeCoverage]
public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.OrderId).HasColumnName("order_id");
        builder.Property(i => i.ProductId).HasColumnName("product_id");
        builder.Property(i => i.ProductName).HasColumnName("product_name").HasMaxLength(200);
        builder.Property(i => i.Quantity).HasColumnName("quantity");
        builder.OwnsOne(i => i.UnitPrice, money => {
            money.Property(m => m.Amount).HasColumnName("unit_price").HasPrecision(10, 2);
        });
        builder.OwnsOne(i => i.TotalPrice, money => {
            money.Property(m => m.Amount).HasColumnName("total_price").HasPrecision(10, 2);
        });
    }
}

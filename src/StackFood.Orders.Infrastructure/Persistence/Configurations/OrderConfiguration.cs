using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackFood.Orders.Domain.Entities;
namespace StackFood.Orders.Infrastructure.Persistence.Configurations;
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");
        builder.Property(o => o.CustomerId).HasColumnName("customer_id");
        builder.Property(o => o.CustomerName).HasColumnName("customer_name").HasMaxLength(200);
        builder.Property(o => o.Status).HasColumnName("status").HasConversion<int>();
        builder.OwnsOne(o => o.TotalAmount, money => {
            money.Property(m => m.Amount).HasColumnName("total_amount").HasPrecision(10, 2);
        });
        builder.Property(o => o.CreatedAt).HasColumnName("created_at");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");
        builder.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey("OrderId");
    }
}

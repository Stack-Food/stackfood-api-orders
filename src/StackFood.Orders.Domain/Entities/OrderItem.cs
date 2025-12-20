using StackFood.Orders.Domain.ValueObjects;

namespace StackFood.Orders.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = null!;
    public Money TotalPrice { get; private set; } = null!;

    // Navigation
    public Order Order { get; private set; } = null!;

    private OrderItem() { }

    public OrderItem(Guid productId, string productName, int quantity, Money unitPrice)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName ?? throw new ArgumentNullException(nameof(productName));
        Quantity = quantity;
        UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
        TotalPrice = unitPrice * quantity;

        Validate();
    }

    public void UpdateQuantity(int quantity)
    {
        Quantity = quantity;
        TotalPrice = UnitPrice * quantity;
        Validate();
    }

    private void Validate()
    {
        if (Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(Quantity));

        if (string.IsNullOrWhiteSpace(ProductName))
            throw new ArgumentException("Product name cannot be empty", nameof(ProductName));
    }
}

using StackFood.Orders.Domain.ValueObjects;

namespace StackFood.Orders.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string? CustomerName { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public Order(Guid? customerId, string? customerName)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        CustomerName = customerName;
        Status = OrderStatus.Pending;
        TotalAmount = new Money(0);
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddItem(Guid productId, string productName, int quantity, Money unitPrice)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot add items to a non-pending order");

        var item = new OrderItem(productId, productName, quantity, unitPrice);
        _items.Add(item);
        RecalculateTotal();
    }

    public void RemoveItem(Guid itemId)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot remove items from a non-pending order");

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _items.Remove(item);
            RecalculateTotal();
        }
    }

    public void ApprovePayment()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot approve payment for order in {Status} status");

        Status = OrderStatus.PaymentApproved;
        UpdatedAt = DateTime.UtcNow;
    }

    public void StartProduction()
    {
        if (Status != OrderStatus.PaymentApproved)
            throw new InvalidOperationException($"Cannot start production for order in {Status} status");

        Status = OrderStatus.InProduction;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsReady()
    {
        if (Status != OrderStatus.InProduction)
            throw new InvalidOperationException($"Cannot mark as ready order in {Status} status");

        Status = OrderStatus.Ready;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != OrderStatus.Ready)
            throw new InvalidOperationException($"Cannot complete order in {Status} status");

        Status = OrderStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed order");

        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateTotal()
    {
        var total = _items.Sum(i => i.TotalPrice.Amount);
        TotalAmount = new Money(total);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Validate()
    {
        if (!_items.Any())
            throw new InvalidOperationException("Order must have at least one item");
    }
}

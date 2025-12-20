namespace StackFood.Orders.Domain.Events;

public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItemEvent> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class OrderItemEvent
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

namespace StackFood.Orders.Domain.Events;

public class OrderCancelledEvent
{
    public Guid OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CancelledAt { get; set; }
}

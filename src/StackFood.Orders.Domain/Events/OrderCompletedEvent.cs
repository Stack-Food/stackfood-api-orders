namespace StackFood.Orders.Domain.Events;

public class OrderCompletedEvent
{
    public Guid OrderId { get; set; }
    public DateTime CompletedAt { get; set; }
}

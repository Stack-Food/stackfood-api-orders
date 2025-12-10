namespace StackFood.Orders.Application.DTOs;
public class CreateOrderRequest
{
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
}
public class OrderItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

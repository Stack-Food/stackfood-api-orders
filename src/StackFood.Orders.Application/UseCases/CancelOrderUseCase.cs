using StackFood.Orders.Application.Interfaces;
using StackFood.Orders.Domain.Events;

namespace StackFood.Orders.Application.UseCases;
public class CancelOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEventPublisher _eventPublisher;

    public CancelOrderUseCase(IOrderRepository orderRepository, IEventPublisher eventPublisher)
    {
        _orderRepository = orderRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task ExecuteAsync(Guid orderId, string reason)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            throw new ArgumentException($"Order {orderId} not found");

        order.Cancel();
        await _orderRepository.UpdateAsync(order);

        await _eventPublisher.PublishAsync("OrderCancelled", new OrderCancelledEvent
        {
            OrderId = orderId,
            Reason = reason,
            CancelledAt = DateTime.UtcNow
        });
    }
}

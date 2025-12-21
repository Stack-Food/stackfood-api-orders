using StackFood.Orders.Application.Interfaces;
using StackFood.Orders.Domain.Entities;

namespace StackFood.Orders.Application.UseCases;

public class UpdateOrderStatusUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEventPublisher _eventPublisher;

    public UpdateOrderStatusUseCase(IOrderRepository orderRepository, IEventPublisher eventPublisher)
    {
        _orderRepository = orderRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task ApprovePaymentAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            throw new ArgumentException($"Order {orderId} not found");

        // Check if already approved to avoid reprocessing old messages
        if (order.Status == OrderStatus.PaymentApproved)
        {
            // Already approved, just publish event again (idempotent)
            await _eventPublisher.PublishAsync("PaymentApproved", new
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                CustomerName = order.CustomerName,
                Items = order.Items.Select(i => new
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice.Amount
                }).ToList(),
                TotalAmount = order.TotalAmount.Amount,
                ApprovedAt = DateTime.UtcNow
            });
            return;
        }

        order.ApprovePayment();
        await _orderRepository.UpdateAsync(order);

        // Publish PaymentApproved event to Production queue
        await _eventPublisher.PublishAsync("PaymentApproved", new
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            Items = order.Items.Select(i => new
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice.Amount
            }).ToList(),
            TotalAmount = order.TotalAmount.Amount,
            ApprovedAt = DateTime.UtcNow
        });
    }

    public async Task StartProductionAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            throw new ArgumentException($"Order {orderId} not found");

        order.StartProduction();
        await _orderRepository.UpdateAsync(order);
    }

    public async Task MarkAsReadyAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            throw new ArgumentException($"Order {orderId} not found");

        order.MarkAsReady();
        await _orderRepository.UpdateAsync(order);
    }

    public async Task CompleteOrderAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            throw new ArgumentException($"Order {orderId} not found");

        order.Complete();
        await _orderRepository.UpdateAsync(order);

        // Publish OrderCompleted event
        await _eventPublisher.PublishAsync("OrderCompleted", new
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            TotalAmount = order.TotalAmount.Amount,
            CompletedAt = DateTime.UtcNow
        });
    }

    public async Task RejectPaymentAsync(Guid orderId, string reason)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            throw new ArgumentException($"Order {orderId} not found");

        order.Cancel();
        await _orderRepository.UpdateAsync(order);
    }
}

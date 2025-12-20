using StackFood.Orders.Application.DTOs;
using StackFood.Orders.Application.Interfaces;
using StackFood.Orders.Application.UseCases.Mappers;
using StackFood.Orders.Domain.Entities;
using StackFood.Orders.Domain.Events;
using StackFood.Orders.Domain.ValueObjects;

namespace StackFood.Orders.Application.UseCases;
public class CreateOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductService _productService;
    private readonly IEventPublisher _eventPublisher;

    public CreateOrderUseCase(IOrderRepository orderRepository, IProductService productService, IEventPublisher eventPublisher)
    {
        _orderRepository = orderRepository;
        _productService = productService;
        _eventPublisher = eventPublisher;
    }

    public async Task<OrderDTO> ExecuteAsync(CreateOrderRequest request)
    {
        var order = new Order(request.CustomerId, request.CustomerName);

        foreach (var item in request.Items)
        {
            var product = await _productService.GetProductByIdAsync(item.ProductId);
            if (product == null)
                throw new ArgumentException($"Product {item.ProductId} not found");
            if (!product.IsAvailable)
                throw new InvalidOperationException($"Product {product.Name} is not available");

            order.AddItem(product.Id, product.Name, item.Quantity, new Money(product.Price));
        }

        order.Validate();
        var createdOrder = await _orderRepository.CreateAsync(order);

        var orderEvent = new OrderCreatedEvent
        {
            OrderId = createdOrder.Id,
            CustomerId = createdOrder.CustomerId,
            CustomerName = createdOrder.CustomerName,
            TotalAmount = createdOrder.TotalAmount.Amount,
            Items = createdOrder.Items.Select(i => new OrderItemEvent
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice.Amount,
                TotalPrice = i.TotalPrice.Amount
            }).ToList(),
            CreatedAt = createdOrder.CreatedAt
        };

        await _eventPublisher.PublishAsync("OrderCreated", orderEvent);

        return OrderMapper.MapToDTO(createdOrder);
    }
}

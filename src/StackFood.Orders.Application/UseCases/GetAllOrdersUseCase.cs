using StackFood.Orders.Application.DTOs;
using StackFood.Orders.Application.Interfaces;
using StackFood.Orders.Domain.Entities;

namespace StackFood.Orders.Application.UseCases;
public class GetAllOrdersUseCase
{
    private readonly IOrderRepository _orderRepository;

    public GetAllOrdersUseCase(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IEnumerable<OrderDTO>> ExecuteAsync(OrderStatus? status = null)
    {
        var orders = status.HasValue 
            ? await _orderRepository.GetByStatusAsync(status.Value)
            : await _orderRepository.GetAllAsync();

        return orders.Select(MapToDTO);
    }

    private static OrderDTO MapToDTO(Order order)
    {
        return new OrderDTO
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount.Amount,
            Items = order.Items.Select(i => new OrderItemDTO
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice.Amount,
                TotalPrice = i.TotalPrice.Amount
            }).ToList(),
            CreatedAt = order.CreatedAt
        };
    }
}

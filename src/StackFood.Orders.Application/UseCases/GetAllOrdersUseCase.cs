using StackFood.Orders.Application.DTOs;
using StackFood.Orders.Application.Interfaces;
using StackFood.Orders.Application.UseCases.Mappers;
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

        return orders.Select(OrderMapper.MapToDTO);
    }
}

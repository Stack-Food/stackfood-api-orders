using StackFood.Orders.Application.DTOs;
using StackFood.Orders.Application.Interfaces;
using StackFood.Orders.Application.UseCases.Mappers;

namespace StackFood.Orders.Application.UseCases;
public class GetOrderByIdUseCase
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdUseCase(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDTO?> ExecuteAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        return order == null ? null : OrderMapper.MapToDTO(order);
    }
}

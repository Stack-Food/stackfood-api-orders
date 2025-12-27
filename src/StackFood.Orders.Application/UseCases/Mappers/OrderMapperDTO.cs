using StackFood.Orders.Application.DTOs;
using StackFood.Orders.Domain.Entities;

namespace StackFood.Orders.Application.UseCases.Mappers;

public static class OrderMapper
{
    public static OrderDTO MapToDTO(Order order)
    {
        return new OrderDTO
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount.Amount,
            Items = [.. order.Items.Select(i => new OrderItemDTO
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice.Amount,
                TotalPrice = i.TotalPrice.Amount
            })],
            CreatedAt = order.CreatedAt
        };
    }
}

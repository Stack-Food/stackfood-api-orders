using Microsoft.AspNetCore.Mvc;
using StackFood.Orders.Application.DTOs;
using StackFood.Orders.Application.UseCases;

namespace StackFood.Orders.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly CreateOrderUseCase _createOrderUseCase;
    private readonly GetOrderByIdUseCase _getOrderByIdUseCase;
    private readonly GetAllOrdersUseCase _getAllOrdersUseCase;
    private readonly CancelOrderUseCase _cancelOrderUseCase;

    public OrdersController(
        CreateOrderUseCase createOrderUseCase,
        GetOrderByIdUseCase getOrderByIdUseCase,
        GetAllOrdersUseCase getAllOrdersUseCase,
        CancelOrderUseCase cancelOrderUseCase)
    {
        _createOrderUseCase = createOrderUseCase;
        _getOrderByIdUseCase = getOrderByIdUseCase;
        _getAllOrdersUseCase = getAllOrdersUseCase;
        _cancelOrderUseCase = cancelOrderUseCase;
    }

    /// <summary>
    /// Creates a new order
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDTO>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            var order = await _createOrderUseCase.ExecuteAsync(request);
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets an order by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDTO>> GetOrderById(Guid id)
    {
        var order = await _getOrderByIdUseCase.ExecuteAsync(id);
        if (order == null)
            return NotFound(new { error = $"Order {id} not found" });

        return Ok(order);
    }

    /// <summary>
    /// Gets all orders
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<OrderDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrderDTO>>> GetAllOrders()
    {
        var orders = await _getAllOrdersUseCase.ExecuteAsync();
        return Ok(orders);
    }

    /// <summary>
    /// Cancels an order
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderRequest? request = null)
    {
        try
        {
            var reason = request?.Reason ?? "Cancelled by user";
            await _cancelOrderUseCase.ExecuteAsync(id, reason);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record CancelOrderRequest(string? Reason);

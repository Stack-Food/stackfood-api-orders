using Microsoft.EntityFrameworkCore;
using StackFood.Orders.Application.Interfaces;
using StackFood.Orders.Domain.Entities;
namespace StackFood.Orders.Infrastructure.Persistence.Repositories;
public class OrderRepository : IOrderRepository
{
    private readonly OrdersDbContext _context;
    public OrderRepository(OrdersDbContext context) => _context = context;
    public async Task<Order?> GetByIdAsync(Guid id) => 
        await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
    public async Task<IEnumerable<Order>> GetAllAsync() => 
        await _context.Orders.Include(o => o.Items).ToListAsync();
    public async Task<IEnumerable<Order>> GetByCustomerIdAsync(Guid customerId) => 
        await _context.Orders.Include(o => o.Items).Where(o => o.CustomerId == customerId).ToListAsync();
    public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status) => 
        await _context.Orders.Include(o => o.Items).Where(o => o.Status == status).ToListAsync();
    public async Task<Order> CreateAsync(Order order) { 
        await _context.Orders.AddAsync(order); 
        await _context.SaveChangesAsync(); 
        return order; 
    }
    public async Task<Order> UpdateAsync(Order order) { 
        _context.Orders.Update(order); 
        await _context.SaveChangesAsync(); 
        return order; 
    }
    public async Task<bool> ExistsAsync(Guid id) => await _context.Orders.AnyAsync(o => o.Id == id);
}

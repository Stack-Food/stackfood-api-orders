namespace StackFood.Orders.Application.Interfaces;
public interface IProductService
{
    Task<ProductInfo?> GetProductByIdAsync(Guid productId);
}
public class ProductInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}

using System.Text.Json;
using StackFood.Orders.Application.Interfaces;
namespace StackFood.Orders.Infrastructure.ExternalServices;
public class ProductService : IProductService
{
    private readonly HttpClient _httpClient;
    public ProductService(HttpClient httpClient) => _httpClient = httpClient;
    public async Task<ProductInfo?> GetProductByIdAsync(Guid productId)
    {
        var response = await _httpClient.GetAsync($"/api/products/{productId}");
        if (!response.IsSuccessStatusCode) return null;
        var content = await response.Content.ReadAsStringAsync();
        var productDto = JsonSerializer.Deserialize<ProductDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return productDto == null ? null : new ProductInfo
        {
            Id = productDto.Id,
            Name = productDto.Name,
            Price = productDto.Price,
            IsAvailable = productDto.IsAvailable
        };
    }
    private class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
    }
}

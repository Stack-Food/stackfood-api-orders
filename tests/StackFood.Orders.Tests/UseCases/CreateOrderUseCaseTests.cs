using FluentAssertions;
using Moq;
using StackFood.Orders.Application.DTOs;
using StackFood.Orders.Application.Interfaces;
using StackFood.Orders.Application.UseCases;
using StackFood.Orders.Domain.Entities;
using StackFood.Orders.Domain.ValueObjects;

namespace StackFood.Orders.Tests.UseCases;

public class CreateOrderUseCaseTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IProductService> _productServiceMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly CreateOrderUseCase _useCase;

    public CreateOrderUseCaseTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _productServiceMock = new Mock<IProductService>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _useCase = new CreateOrderUseCase(
            _orderRepositoryMock.Object,
            _productServiceMock.Object,
            _eventPublisherMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ShouldCreateOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            CustomerName = "John Doe",
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = productId, Quantity = 2 }
            }
        };

        var productInfo = new ProductInfo
        {
            Id = productId,
            Name = "Product 1",
            Price = 10.50m,
            IsAvailable = true
        };

        _productServiceMock
            .Setup(x => x.GetProductByIdAsync(productId))
            .ReturnsAsync(productInfo);

        _orderRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.CustomerId.Should().Be(customerId);
        result.CustomerName.Should().Be("John Doe");
        result.TotalAmount.Should().Be(21.00m);
        result.Items.Should().HaveCount(1);
        result.Status.Should().Be("Pending");

        _eventPublisherMock.Verify(
            x => x.PublishAsync("OrderCreated", It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnavailableProduct_ShouldThrowException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "John Doe",
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = productId, Quantity = 2 }
            }
        };

        var productInfo = new ProductInfo
        {
            Id = productId,
            Name = "Product 1",
            Price = 10.50m,
            IsAvailable = false
        };

        _productServiceMock
            .Setup(x => x.GetProductByIdAsync(productId))
            .ReturnsAsync(productInfo);

        // Act & Assert
        var act = async () => await _useCase.ExecuteAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Product Product 1 is not available");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentProduct_ShouldThrowException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "John Doe",
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = productId, Quantity = 2 }
            }
        };

        _productServiceMock
            .Setup(x => x.GetProductByIdAsync(productId))
            .ReturnsAsync((ProductInfo?)null);

        // Act & Assert
        var act = async () => await _useCase.ExecuteAsync(request);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Product {productId} not found");
    }
}

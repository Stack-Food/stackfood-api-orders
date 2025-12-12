using FluentAssertions;
using Moq;
using StackFood.Orders.Application.Interfaces;
using StackFood.Orders.Application.UseCases;
using StackFood.Orders.Domain.Entities;
using StackFood.Orders.Domain.ValueObjects;

namespace StackFood.Orders.Tests.UseCases;

public class GetOrderByIdUseCaseTests
{
    private readonly Mock<IOrderRepository> _repositoryMock;
    private readonly GetOrderByIdUseCase _useCase;

    public GetOrderByIdUseCaseTests()
    {
        _repositoryMock = new Mock<IOrderRepository>();
        _useCase = new GetOrderByIdUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrderExists_ShouldReturnOrderDTO()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateSampleOrder();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _useCase.ExecuteAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
        result.CustomerId.Should().Be(order.CustomerId);
        result.CustomerName.Should().Be(order.CustomerName);
        result.Status.Should().Be(order.Status.ToString());
        result.TotalAmount.Should().Be(order.TotalAmount.Amount);
        result.Items.Should().HaveCount(order.Items.Count);

        _repositoryMock.Verify(r => r.GetByIdAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrderNotFound_ShouldReturnNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _useCase.ExecuteAsync(orderId);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(r => r.GetByIdAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMapAllOrderProperties()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var order = new Order(customerId, "Jane Doe");

        order.AddItem(
            Guid.NewGuid(),
            "Product A",
            2,
            new Money(15.50m)
        );
        order.AddItem(
            Guid.NewGuid(),
            "Product B",
            1,
            new Money(25.00m)
        );

        _repositoryMock
            .Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _useCase.ExecuteAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);

        var dtoItem1 = result.Items.First();
        dtoItem1.ProductName.Should().Be("Product A");
        dtoItem1.Quantity.Should().Be(2);
        dtoItem1.UnitPrice.Should().Be(15.50m);
        dtoItem1.TotalPrice.Should().Be(31.00m);

        var dtoItem2 = result.Items.Last();
        dtoItem2.ProductName.Should().Be("Product B");
        dtoItem2.Quantity.Should().Be(1);
        dtoItem2.UnitPrice.Should().Be(25.00m);
        dtoItem2.TotalPrice.Should().Be(25.00m);
    }

    [Fact]
    public async Task ExecuteAsync_WithDifferentStatuses_ShouldMapCorrectly()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateSampleOrder();
        order.ApprovePayment();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _useCase.ExecuteAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("PaymentApproved");
    }

    private Order CreateSampleOrder()
    {
        var customerId = Guid.NewGuid();
        var order = new Order(customerId, "John Doe");

        order.AddItem(
            Guid.NewGuid(),
            "Sample Product",
            3,
            new Money(20.00m)
        );

        return order;
    }
}

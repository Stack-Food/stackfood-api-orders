using FluentAssertions;
using Moq;
using StackFood.Orders.Application.Interfaces;
using StackFood.Orders.Application.UseCases;
using StackFood.Orders.Domain.Entities;
using StackFood.Orders.Domain.ValueObjects;

namespace StackFood.Orders.Tests.UseCases;

public class CancelOrderUseCaseTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly CancelOrderUseCase _useCase;

    public CancelOrderUseCaseTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _useCase = new CancelOrderUseCase(
            _orderRepositoryMock.Object,
            _eventPublisherMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidOrder_ShouldCancelOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(Guid.NewGuid(), "John Doe");
        order.AddItem(Guid.NewGuid(), "Product 1", 2, new Money(10));

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        _orderRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);

        // Act
        await _useCase.ExecuteAsync(orderId, "Customer requested cancellation");

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);

        _eventPublisherMock.Verify(
            x => x.PublishAsync("OrderCancelled", It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentOrder_ShouldThrowException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        var act = async () => await _useCase.ExecuteAsync(orderId, "Test reason");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Order {orderId} not found");
    }

    [Fact]
    public async Task ExecuteAsync_WithCompletedOrder_ShouldThrowException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(Guid.NewGuid(), "John Doe");
        order.AddItem(Guid.NewGuid(), "Product 1", 2, new Money(10));
        order.ApprovePayment();
        order.StartProduction();
        order.MarkAsReady();
        order.Complete();

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act & Assert
        var act = async () => await _useCase.ExecuteAsync(orderId, "Test reason");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot cancel a completed order");
    }
}

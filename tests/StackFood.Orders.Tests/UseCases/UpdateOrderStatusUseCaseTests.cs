using FluentAssertions;
using Moq;
using StackFood.Orders.Application.Interfaces;
using StackFood.Orders.Application.UseCases;
using StackFood.Orders.Domain.Entities;
using StackFood.Orders.Domain.ValueObjects;

namespace StackFood.Orders.Tests.UseCases;

public class UpdateOrderStatusUseCaseTests
{
    private readonly Mock<IOrderRepository> _repositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly UpdateOrderStatusUseCase _useCase;

    public UpdateOrderStatusUseCaseTests()
    {
        _repositoryMock = new Mock<IOrderRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _useCase = new UpdateOrderStatusUseCase(_repositoryMock.Object, _eventPublisherMock.Object);
    }

    [Fact]
    public async Task ApprovePaymentAsync_WhenOrderExists_ShouldUpdateStatusAndPublishEvent()
    {
        // Arrange
        var order = CreateSampleOrder();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        // Act
        await _useCase.ApprovePaymentAsync(order.Id);

        // Assert
        order.Status.Should().Be(OrderStatus.PaymentApproved);
        _repositoryMock.Verify(r => r.UpdateAsync(order), Times.Once);
        _eventPublisherMock.Verify(
            e => e.PublishAsync("PaymentApproved", It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ApprovePaymentAsync_WhenOrderAlreadyApproved_ShouldBeIdempotentAndPublishEvent()
    {
        // Arrange
        var order = CreateSampleOrder();
        order.ApprovePayment(); // Already approved

        _repositoryMock
            .Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        // Act
        await _useCase.ApprovePaymentAsync(order.Id);

        // Assert
        order.Status.Should().Be(OrderStatus.PaymentApproved);
        // Should NOT update the order again (idempotent)
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        // Should still publish the event (idempotent event publishing)
        _eventPublisherMock.Verify(
            e => e.PublishAsync("PaymentApproved", It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ApprovePaymentAsync_WhenOrderNotFound_ShouldThrowException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var act = () => _useCase.ApprovePaymentAsync(orderId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Order {orderId} not found");
    }

    [Fact]
    public async Task StartProductionAsync_WhenOrderExists_ShouldUpdateStatus()
    {
        // Arrange
        var order = CreateSampleOrder();
        order.ApprovePayment();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        // Act
        await _useCase.StartProductionAsync(order.Id);

        // Assert
        order.Status.Should().Be(OrderStatus.InProduction);
        _repositoryMock.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task StartProductionAsync_WhenOrderNotFound_ShouldThrowException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var act = () => _useCase.StartProductionAsync(orderId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Order {orderId} not found");
    }

    [Fact]
    public async Task MarkAsReadyAsync_WhenOrderExists_ShouldUpdateStatus()
    {
        // Arrange
        var order = CreateSampleOrder();
        order.ApprovePayment();
        order.StartProduction();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        // Act
        await _useCase.MarkAsReadyAsync(order.Id);

        // Assert
        order.Status.Should().Be(OrderStatus.Ready);
        _repositoryMock.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadyAsync_WhenOrderNotFound_ShouldThrowException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var act = () => _useCase.MarkAsReadyAsync(orderId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Order {orderId} not found");
    }

    [Fact]
    public async Task CompleteOrderAsync_WhenOrderExists_ShouldUpdateStatusAndPublishEvent()
    {
        // Arrange
        var order = CreateSampleOrder();
        order.ApprovePayment();
        order.StartProduction();
        order.MarkAsReady();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        // Act
        await _useCase.CompleteOrderAsync(order.Id);

        // Assert
        order.Status.Should().Be(OrderStatus.Completed);
        _repositoryMock.Verify(r => r.UpdateAsync(order), Times.Once);
        _eventPublisherMock.Verify(
            e => e.PublishAsync("OrderCompleted", It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task CompleteOrderAsync_WhenOrderNotFound_ShouldThrowException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var act = () => _useCase.CompleteOrderAsync(orderId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Order {orderId} not found");
    }

    [Fact]
    public async Task RejectPaymentAsync_WhenOrderExists_ShouldCancelOrder()
    {
        // Arrange
        var order = CreateSampleOrder();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        // Act
        await _useCase.RejectPaymentAsync(order.Id, "Insufficient funds");

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
        _repositoryMock.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task RejectPaymentAsync_WhenOrderNotFound_ShouldThrowException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var act = () => _useCase.RejectPaymentAsync(orderId, "Test reason");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Order {orderId} not found");
    }

    private Order CreateSampleOrder()
    {
        var customerId = Guid.NewGuid();
        var order = new Order(customerId, "John Doe");

        order.AddItem(
            Guid.NewGuid(),
            "Test Product",
            2,
            new Money(10.00m)
        );

        return order;
    }
}

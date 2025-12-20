using FluentAssertions;
using Moq;
using StackFood.Orders.Application.Interfaces;
using StackFood.Orders.Application.UseCases;
using StackFood.Orders.Domain.Entities;
using StackFood.Orders.Domain.ValueObjects;

namespace StackFood.Orders.Tests.UseCases;

public class GetAllOrdersUseCaseTests
{
    private readonly Mock<IOrderRepository> _repositoryMock;
    private readonly GetAllOrdersUseCase _useCase;

    public GetAllOrdersUseCaseTests()
    {
        _repositoryMock = new Mock<IOrderRepository>();
        _useCase = new GetAllOrdersUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutStatus_ShouldReturnAllOrders()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateSampleOrder(OrderStatus.Pending),
            CreateSampleOrder(OrderStatus.PaymentApproved),
            CreateSampleOrder(OrderStatus.Completed)
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(orders);

        // Act
        var result = await _useCase.ExecuteAsync();

        // Assert
        result.Should().HaveCount(3);
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
        _repositoryMock.Verify(r => r.GetByStatusAsync(It.IsAny<OrderStatus>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithStatus_ShouldReturnFilteredOrders()
    {
        // Arrange
        var pendingOrders = new List<Order>
        {
            CreateSampleOrder(OrderStatus.Pending),
            CreateSampleOrder(OrderStatus.Pending)
        };

        _repositoryMock
            .Setup(r => r.GetByStatusAsync(OrderStatus.Pending))
            .ReturnsAsync(pendingOrders);

        // Act
        var result = await _useCase.ExecuteAsync(OrderStatus.Pending);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(o => o.Status.Should().Be("Pending"));
        _repositoryMock.Verify(r => r.GetByStatusAsync(OrderStatus.Pending), Times.Once);
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMapOrdersToDTO()
    {
        // Arrange
        var order = CreateSampleOrder(OrderStatus.PaymentApproved);
        _repositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Order> { order });

        // Act
        var result = (await _useCase.ExecuteAsync()).ToList();

        // Assert
        result.Should().HaveCount(1);
        var dto = result.First();
        dto.Id.Should().Be(order.Id);
        dto.CustomerId.Should().Be(order.CustomerId);
        dto.CustomerName.Should().Be(order.CustomerName);
        dto.Status.Should().Be("PaymentApproved");
        dto.TotalAmount.Should().Be(order.TotalAmount.Amount);
        dto.Items.Should().HaveCount(order.Items.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyRepository_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Order>());

        // Act
        var result = await _useCase.ExecuteAsync();

        // Assert
        result.Should().BeEmpty();
    }

    private Order CreateSampleOrder(OrderStatus status)
    {
        var customerId = Guid.NewGuid();
        var order = new Order(customerId, "John Doe");

        order.AddItem(
            Guid.NewGuid(),
            "Product 1",
            2,
            new Money(10.00m)
        );

        // Set status using reflection or helper
        if (status == OrderStatus.PaymentApproved)
            order.ApprovePayment();
        else if (status == OrderStatus.InProduction)
        {
            order.ApprovePayment();
            order.StartProduction();
        }
        else if (status == OrderStatus.Ready)
        {
            order.ApprovePayment();
            order.StartProduction();
            order.MarkAsReady();
        }
        else if (status == OrderStatus.Completed)
        {
            order.ApprovePayment();
            order.StartProduction();
            order.MarkAsReady();
            order.Complete();
        }
        else if (status == OrderStatus.Cancelled)
            order.Cancel();

        return order;
    }
}

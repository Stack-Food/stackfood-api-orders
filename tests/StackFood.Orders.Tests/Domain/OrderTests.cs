using FluentAssertions;
using StackFood.Orders.Domain.Entities;
using StackFood.Orders.Domain.ValueObjects;

namespace StackFood.Orders.Tests.Domain;

public class OrderTests
{
    [Fact]
    public void Constructor_ShouldCreateOrderWithPendingStatus()
    {
        // Arrange & Act
        var order = new Order(Guid.NewGuid(), "John Doe");

        // Assert
        order.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Amount.Should().Be(0);
        order.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_ShouldAddItemToOrder()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), "John Doe");
        var productId = Guid.NewGuid();
        var unitPrice = new Money(10.50m);

        // Act
        order.AddItem(productId, "Product 1", 2, unitPrice);

        // Assert
        order.Items.Should().HaveCount(1);
        order.TotalAmount.Amount.Should().Be(21.00m);
    }

    [Fact]
    public void AddItem_WhenNotPending_ShouldThrowException()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), "John Doe");
        order.AddItem(Guid.NewGuid(), "Product", 1, new Money(10));
        order.ApprovePayment();

        // Act & Assert
        var act = () => order.AddItem(Guid.NewGuid(), "Product 2", 1, new Money(5));
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot add items to a non-pending order");
    }

    [Fact]
    public void ApprovePayment_ShouldChangeStatusToPaymentApproved()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), "John Doe");
        order.AddItem(Guid.NewGuid(), "Product", 1, new Money(10));

        // Act
        order.ApprovePayment();

        // Assert
        order.Status.Should().Be(OrderStatus.PaymentApproved);
    }

    [Fact]
    public void StartProduction_ShouldChangeStatusToInProduction()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), "John Doe");
        order.AddItem(Guid.NewGuid(), "Product", 1, new Money(10));
        order.ApprovePayment();

        // Act
        order.StartProduction();

        // Assert
        order.Status.Should().Be(OrderStatus.InProduction);
    }

    [Fact]
    public void Complete_ShouldChangeStatusToCompleted()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), "John Doe");
        order.AddItem(Guid.NewGuid(), "Product", 1, new Money(10));
        order.ApprovePayment();
        order.StartProduction();
        order.MarkAsReady();

        // Act
        order.Complete();

        // Assert
        order.Status.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public void Cancel_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), "John Doe");
        order.AddItem(Guid.NewGuid(), "Product", 1, new Money(10));

        // Act
        order.Cancel();

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenCompleted_ShouldThrowException()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), "John Doe");
        order.AddItem(Guid.NewGuid(), "Product", 1, new Money(10));
        order.ApprovePayment();
        order.StartProduction();
        order.MarkAsReady();
        order.Complete();

        // Act & Assert
        var act = () => order.Cancel();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot cancel a completed order");
    }

    [Fact]
    public void Validate_WithNoItems_ShouldThrowException()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), "John Doe");

        // Act & Assert
        var act = () => order.Validate();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Order must have at least one item");
    }
}

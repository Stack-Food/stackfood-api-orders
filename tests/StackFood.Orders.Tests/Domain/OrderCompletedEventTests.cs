using FluentAssertions;
using StackFood.Orders.Domain.Events;

namespace StackFood.Orders.Tests.Domain;

public class OrderCompletedEventTests
{
    [Fact]
    public void Constructor_ShouldCreateEventWithDefaultValues()
    {
        // Act
        var orderEvent = new OrderCompletedEvent();

        // Assert
        orderEvent.Should().NotBeNull();
        orderEvent.OrderId.Should().Be(Guid.Empty);
        orderEvent.CompletedAt.Should().Be(default(DateTime));
    }

    [Fact]
    public void OrderId_ShouldSetAndGetValue()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orderEvent = new OrderCompletedEvent();

        // Act
        orderEvent.OrderId = orderId;

        // Assert
        orderEvent.OrderId.Should().Be(orderId);
    }

    [Fact]
    public void CompletedAt_ShouldSetAndGetValue()
    {
        // Arrange
        var completedAt = DateTime.UtcNow;
        var orderEvent = new OrderCompletedEvent();

        // Act
        orderEvent.CompletedAt = completedAt;

        // Assert
        orderEvent.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void Event_ShouldAllowMultiplePropertySets()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var completedAt = DateTime.UtcNow;

        // Act
        var orderEvent = new OrderCompletedEvent
        {
            OrderId = orderId,
            CompletedAt = completedAt
        };

        // Assert
        orderEvent.OrderId.Should().Be(orderId);
        orderEvent.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void Event_ShouldSupportObjectInitializer()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var completedAt = new DateTime(2024, 1, 15, 10, 30, 0);

        // Act
        var orderEvent = new OrderCompletedEvent
        {
            OrderId = orderId,
            CompletedAt = completedAt
        };

        // Assert
        orderEvent.Should().NotBeNull();
        orderEvent.OrderId.Should().Be(orderId);
        orderEvent.CompletedAt.Should().Be(completedAt);
    }
}

using FluentAssertions;
using StackFood.Orders.Domain.Entities;
using StackFood.Orders.Domain.ValueObjects;

namespace StackFood.Orders.Tests.Domain;

public class OrderItemTests
{
    [Fact]
    public void Constructor_ShouldCreateOrderItemWithCorrectValues()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productName = "Hambúrguer";
        var quantity = 2;
        var unitPrice = new Money(15.50m);

        // Act
        var orderItem = new OrderItem(productId, productName, quantity, unitPrice);

        // Assert
        orderItem.Id.Should().NotBe(Guid.Empty);
        orderItem.ProductId.Should().Be(productId);
        orderItem.ProductName.Should().Be(productName);
        orderItem.Quantity.Should().Be(quantity);
        orderItem.UnitPrice.Should().Be(unitPrice);
        orderItem.TotalPrice.Amount.Should().Be(31.00m);
    }

    [Fact]
    public void Constructor_ShouldCalculateTotalPrice()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var unitPrice = new Money(10.50m);
        var quantity = 3;

        // Act
        var orderItem = new OrderItem(productId, "Product", quantity, unitPrice);

        // Assert
        orderItem.TotalPrice.Amount.Should().Be(31.50m);
    }

    [Fact]
    public void Constructor_WithNullProductName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var unitPrice = new Money(10.50m);

        // Act & Assert
        var act = () => new OrderItem(productId, null!, 1, unitPrice);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("productName");
    }

    [Fact]
    public void Constructor_WithEmptyProductName_ShouldThrowArgumentException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var unitPrice = new Money(10.50m);

        // Act & Assert
        var act = () => new OrderItem(productId, "", 1, unitPrice);
        act.Should().Throw<ArgumentException>()
            .Where(ex => ex.Message.Contains("Product name cannot be empty"));
    }

    [Fact]
    public void Constructor_WithWhitespaceProductName_ShouldThrowArgumentException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var unitPrice = new Money(10.50m);

        // Act & Assert
        var act = () => new OrderItem(productId, "   ", 1, unitPrice);
        act.Should().Throw<ArgumentException>()
            .Where(ex => ex.Message.Contains("Product name cannot be empty"));
    }

    [Fact]
    public void Constructor_WithNullUnitPrice_ShouldThrowArgumentNullException()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act & Assert
        var act = () => new OrderItem(productId, "Product", 1, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("unitPrice");
    }

    [Fact]
    public void Constructor_WithZeroQuantity_ShouldThrowArgumentException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var unitPrice = new Money(10.50m);

        // Act & Assert
        var act = () => new OrderItem(productId, "Product", 0, unitPrice);
        act.Should().Throw<ArgumentException>()
            .Where(ex => ex.Message.Contains("Quantity must be greater than zero"))
            .WithParameterName("Quantity");
    }

    [Fact]
    public void Constructor_WithNegativeQuantity_ShouldThrowArgumentException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var unitPrice = new Money(10.50m);

        // Act & Assert
        // Negative quantity causes Money multiplication to throw (unitPrice * -1 = negative amount)
        var act = () => new OrderItem(productId, "Product", -1, unitPrice);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateQuantity_ShouldUpdateQuantityAndRecalculateTotalPrice()
    {
        // Arrange
        var orderItem = new OrderItem(Guid.NewGuid(), "Product", 2, new Money(10.50m));

        // Act
        orderItem.UpdateQuantity(5);

        // Assert
        orderItem.Quantity.Should().Be(5);
        orderItem.TotalPrice.Amount.Should().Be(52.50m);
    }

    [Fact]
    public void UpdateQuantity_WithZero_ShouldThrowArgumentException()
    {
        // Arrange
        var orderItem = new OrderItem(Guid.NewGuid(), "Product", 2, new Money(10.50m));

        // Act & Assert
        var act = () => orderItem.UpdateQuantity(0);
        act.Should().Throw<ArgumentException>()
            .Where(ex => ex.Message.Contains("Quantity must be greater than zero"));
    }

    [Fact]
    public void UpdateQuantity_WithNegativeValue_ShouldThrowArgumentException()
    {
        // Arrange
        var orderItem = new OrderItem(Guid.NewGuid(), "Product", 2, new Money(10.50m));

        // Act & Assert
        // Negative quantity causes Money multiplication to throw (unitPrice * -3 = negative amount)
        var act = () => orderItem.UpdateQuantity(-3);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateQuantity_ShouldMaintainUnitPriceUnchanged()
    {
        // Arrange
        var unitPrice = new Money(10.50m);
        var orderItem = new OrderItem(Guid.NewGuid(), "Product", 2, unitPrice);

        // Act
        orderItem.UpdateQuantity(4);

        // Assert
        orderItem.UnitPrice.Should().Be(unitPrice);
    }
}

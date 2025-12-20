using FluentAssertions;
using StackFood.Orders.Domain.ValueObjects;

namespace StackFood.Orders.Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Constructor_ShouldCreateMoneyWithAmount()
    {
        // Act
        var money = new Money(10.50m);

        // Assert
        money.Amount.Should().Be(10.50m);
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ShouldThrowException()
    {
        // Act & Assert
        var act = () => new Money(-10);
        act.Should().Throw<ArgumentException>()
            .Where(ex => ex.Message.Contains("Amount cannot be negative"));
    }

    [Fact]
    public void Addition_ShouldAddTwoMoneyValues()
    {
        // Arrange
        var money1 = new Money(10.50m);
        var money2 = new Money(5.25m);

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.Should().Be(15.75m);
    }

    [Fact]
    public void Multiplication_ShouldMultiplyMoneyByInteger()
    {
        // Arrange
        var money = new Money(10.50m);

        // Act
        var result = money * 3;

        // Assert
        result.Amount.Should().Be(31.50m);
    }

    [Fact]
    public void Equals_ShouldReturnTrueForSameAmount()
    {
        // Arrange
        var money1 = new Money(10.50m);
        var money2 = new Money(10.50m);

        // Act & Assert
        money1.Should().Be(money2);
    }

    [Fact]
    public void Equals_ShouldReturnFalseForDifferentAmount()
    {
        // Arrange
        var money1 = new Money(10.50m);
        var money2 = new Money(5.25m);

        // Act & Assert
        money1.Should().NotBe(money2);
    }
}

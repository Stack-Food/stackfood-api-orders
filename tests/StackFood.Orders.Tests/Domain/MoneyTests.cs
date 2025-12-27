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

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var money = new Money(10.50m);

        // Act & Assert
        money.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNonMoneyObject_ShouldReturnFalse()
    {
        // Arrange
        var money = new Money(10.50m);
        var notMoney = "not a money object";

        // Act & Assert
        money.Equals(notMoney).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldReturnSameHashForEqualAmounts()
    {
        // Arrange
        var money1 = new Money(10.50m);
        var money2 = new Money(10.50m);

        // Act & Assert
        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ShouldReturnDifferentHashForDifferentAmounts()
    {
        // Arrange
        var money1 = new Money(10.50m);
        var money2 = new Money(5.25m);

        // Act
        var hash1 = money1.GetHashCode();
        var hash2 = money2.GetHashCode();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ToString_ShouldFormatAmountWithTwoDecimals()
    {
        // Arrange
        var money = new Money(10.5m);

        // Act
        var result = money.ToString();

        // Assert
        result.Should().MatchRegex(@"^10[,\.]50$");
    }

    [Fact]
    public void ToString_ShouldFormatAmountWithRounding()
    {
        // Arrange
        var money = new Money(10.567m);

        // Act
        var result = money.ToString();

        // Assert
        result.Should().MatchRegex(@"^10[,\.]57$");
    }

    [Fact]
    public void Constructor_ShouldRoundToTwoDecimals()
    {
        // Arrange & Act
        var money = new Money(10.567m);

        // Assert
        money.Amount.Should().Be(10.57m);
    }

    [Fact]
    public void Multiplication_WithDecimal_ShouldMultiplyCorrectly()
    {
        // Arrange
        var money = new Money(10.50m);

        // Act
        var result = money * 2.5m;

        // Assert
        result.Amount.Should().Be(26.25m);
    }

    [Fact]
    public void Multiplication_WithZero_ShouldReturnZero()
    {
        // Arrange
        var money = new Money(10.50m);

        // Act
        var result = money * 0;

        // Assert
        result.Amount.Should().Be(0m);
    }

    [Fact]
    public void Addition_WithZero_ShouldReturnOriginalAmount()
    {
        // Arrange
        var money1 = new Money(10.50m);
        var money2 = new Money(0m);

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.Should().Be(10.50m);
    }
}

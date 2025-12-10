namespace StackFood.Orders.Domain.ValueObjects;

public class Money : IEquatable<Money>
{
    public decimal Amount { get; private set; }
    private Money() { }
    public Money(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        Amount = decimal.Round(amount, 2);
    }
    public static Money operator +(Money left, Money right) => new Money(left.Amount + right.Amount);
    public static Money operator *(Money money, decimal multiplier) => new Money(money.Amount * multiplier);
    public bool Equals(Money? other) => other != null && Amount == other.Amount;
    public override bool Equals(object? obj) => Equals(obj as Money);
    public override int GetHashCode() => Amount.GetHashCode();
    public override string ToString() => Amount.ToString("F2");
}

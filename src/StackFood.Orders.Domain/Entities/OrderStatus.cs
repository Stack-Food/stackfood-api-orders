namespace StackFood.Orders.Domain.Entities;

public enum OrderStatus
{
    Pending = 0,
    PaymentApproved = 1,
    InProduction = 2,
    Ready = 3,
    Completed = 4,
    Cancelled = 5
}

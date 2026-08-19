namespace Restaurant.Application;

public sealed class OrderConcurrencyException : Exception
{
    public OrderConcurrencyException()
        : base("The order was changed by another operation.")
    {
    }
}

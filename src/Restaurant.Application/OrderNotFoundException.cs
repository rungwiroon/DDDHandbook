namespace Restaurant.Application;

public sealed class OrderNotFoundException : Exception
{
    public OrderNotFoundException() : base("Order was not found.")
    {
    }
}

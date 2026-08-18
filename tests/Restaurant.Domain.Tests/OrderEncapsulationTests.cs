using Restaurant.Domain;

namespace Restaurant.Domain.Tests;

public sealed class OrderEncapsulationTests
{
    [Fact]
    public void Lines_cannot_be_mutated_outside_the_aggregate()
    {
        var order = Order.Create(OrderId.From(Guid.NewGuid()), TableNumber.From(1));

        var lines = Assert.IsAssignableFrom<IList<OrderLine>>(order.Lines);

        Assert.Throws<NotSupportedException>(() => lines.Add(null!));
        Assert.Empty(order.Lines);
    }
}

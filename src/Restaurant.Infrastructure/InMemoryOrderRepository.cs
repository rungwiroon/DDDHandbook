using Restaurant.Application;
using Restaurant.Domain;

namespace Restaurant.Infrastructure;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<OrderId, Order> _orders = [];

    public Order? Find(OrderId id) => _orders.GetValueOrDefault(id);

    public void Add(Order order) => _orders.Add(order.Id, order);

    public void Save(Order order, IReadOnlyCollection<IDomainEvent>? events = null)
    {
    }
}

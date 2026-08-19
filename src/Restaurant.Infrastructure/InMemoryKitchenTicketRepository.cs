using Restaurant.Application;
using Restaurant.Domain;

namespace Restaurant.Infrastructure;

public sealed class InMemoryKitchenTicketRepository : IKitchenTicketRepository
{
    private readonly Dictionary<OrderId, KitchenTicket> _tickets = [];

    public KitchenTicket? Find(OrderId orderId) => _tickets.GetValueOrDefault(orderId);

    public IReadOnlyCollection<KitchenTicket> All() => _tickets.Values.ToArray();

    public void Add(KitchenTicket ticket) => _tickets.Add(ticket.OrderId, ticket);

    public void Save(KitchenTicket ticket)
    {
    }
}

using Restaurant.Domain;

namespace Restaurant.Application;

public interface IKitchenTicketRepository
{
    KitchenTicket? Find(OrderId orderId);

    IReadOnlyCollection<KitchenTicket> All();

    void Add(KitchenTicket ticket);
}

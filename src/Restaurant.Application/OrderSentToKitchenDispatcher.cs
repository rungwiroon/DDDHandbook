using Restaurant.Domain;

namespace Restaurant.Application;

public sealed class OrderSentToKitchenDispatcher(IKitchenTicketRepository kitchenTickets) : IDomainEventDispatcher
{
    public void Dispatch(IDomainEvent domainEvent)
    {
        if (domainEvent is OrderSentToKitchen sent)
        {
            kitchenTickets.Add(KitchenTicket.Create(sent));
            return;
        }

        throw new InvalidOperationException($"No handler is registered for {domainEvent.GetType().Name}.");
    }
}

using Restaurant.Domain;

namespace Restaurant.Application;

public sealed class OrderSentToKitchenDispatcher(IKitchenTicketRepository kitchenTickets) : IDomainEventDispatcher
{
    public void Dispatch(IDomainEvent domainEvent)
    {
        if (domainEvent is OrderSentToKitchen sent)
        {
            var ticket = KitchenTicket.Create(sent);
            kitchenTickets.Add(ticket);
            kitchenTickets.Save(ticket);
            return;
        }

        throw new InvalidOperationException($"No handler is registered for {domainEvent.GetType().Name}.");
    }
}

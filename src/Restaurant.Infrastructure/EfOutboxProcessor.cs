using System.Text.Json;
using Restaurant.Application;
using Restaurant.Domain;

namespace Restaurant.Infrastructure;

public sealed class EfOutboxProcessor(RestaurantDbContext db, IDomainEventDispatcher dispatcher) : IOutboxProcessor
{
    public void ProcessPending()
    {
        var messages = db.Outbox
            .Where(message => message.ProcessedUtc == null)
            .OrderBy(message => message.OccurredUtc)
            .ToArray();

        foreach (var message in messages)
        {
            dispatcher.Dispatch(Deserialize(message));
            message.ProcessedUtc = DateTime.UtcNow;
            db.SaveChanges();
        }
    }

    private static IDomainEvent Deserialize(OutboxRecord message)
    {
        if (message.Type != nameof(OrderSentToKitchen))
        {
            throw new InvalidOperationException($"No handler is registered for {message.Type}.");
        }

        var payload = JsonSerializer.Deserialize<OrderSentPayload>(message.Payload)
            ?? throw new InvalidOperationException("Outbox payload was empty.");

        return new OrderSentToKitchen(
            OrderId.From(payload.OrderId),
            TableNumber.From(payload.TableNumber),
            [.. payload.Lines.Select(line => new OrderLineSnapshot(
                MenuItemId.From(line.MenuItemId),
                line.ItemName,
                Quantity.From(line.Quantity)))]);
    }

    private sealed record OrderLinePayload(Guid MenuItemId, string ItemName, int Quantity);

    private sealed record OrderSentPayload(Guid OrderId, int TableNumber, OrderLinePayload[] Lines);
}

using System.Collections.Immutable;

namespace Restaurant.Domain;

public sealed class KitchenTicket
{
    private KitchenTicket(OrderId orderId, TableNumber tableNumber, ImmutableArray<OrderLineSnapshot> lines)
    {
        OrderId = orderId;
        TableNumber = tableNumber;
        Lines = lines;
    }

    public OrderId OrderId { get; }

    public TableNumber TableNumber { get; }

    public ImmutableArray<OrderLineSnapshot> Lines { get; }

    public static KitchenTicket Create(OrderSentToKitchen sent) =>
        new(sent.OrderId, sent.TableNumber, sent.Lines);

    public static KitchenTicket Rehydrate(
        OrderId orderId,
        TableNumber tableNumber,
        IEnumerable<OrderLineSnapshot> lines) =>
        new(orderId, tableNumber, [.. lines]);
}

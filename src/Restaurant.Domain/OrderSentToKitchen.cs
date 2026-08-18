using System.Collections.Immutable;

namespace Restaurant.Domain;

public sealed record OrderLineSnapshot(
    MenuItemId MenuItemId,
    string ItemName,
    Quantity Quantity);

public sealed record OrderSentToKitchen(
    OrderId OrderId,
    TableNumber TableNumber,
    ImmutableArray<OrderLineSnapshot> Lines) : IDomainEvent
{
    public static OrderSentToKitchen From(Order order) => new(
        order.Id,
        order.TableNumber,
        [.. order.Lines.Select(line => new OrderLineSnapshot(
            line.MenuItemId,
            line.ItemName,
            line.Quantity))]);
}

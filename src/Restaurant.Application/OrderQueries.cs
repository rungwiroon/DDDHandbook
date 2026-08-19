using Restaurant.Domain;

namespace Restaurant.Application;

public sealed record OrderLineDto(
    Guid MenuItemId,
    string ItemName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal);

public sealed record OrderDetailDto(
    Guid OrderId,
    int TableNumber,
    string Status,
    IReadOnlyList<OrderLineDto> Lines,
    decimal Total);

public sealed class OrderQueryService(IOrderRepository orders)
{
    public OrderDetailDto Get(OrderId orderId)
    {
        var order = orders.Find(orderId) ?? throw new OrderNotFoundException();
        return new OrderDetailDto(
            order.Id.Value,
            order.TableNumber.Value,
            order.Status.ToString(),
            order.Lines.Select(line => new OrderLineDto(
                line.MenuItemId.Value,
                line.ItemName,
                line.UnitPrice.Value,
                line.Quantity.Value,
                line.Subtotal.Value)).ToArray(),
            order.Total.Value);
    }
}

public sealed record KitchenBoardLineDto(Guid MenuItemId, string ItemName, int Quantity);

public sealed record KitchenBoardTicketDto(
    Guid OrderId,
    int TableNumber,
    IReadOnlyList<KitchenBoardLineDto> Lines);

public sealed class KitchenBoardQueryService(IKitchenTicketRepository tickets)
{
    public IReadOnlyList<KitchenBoardTicketDto> Get()
    {
        return tickets.All()
            .Select(ticket => new KitchenBoardTicketDto(
                ticket.OrderId.Value,
                ticket.TableNumber.Value,
                ticket.Lines.Select(line => new KitchenBoardLineDto(
                    line.MenuItemId.Value,
                    line.ItemName,
                    line.Quantity.Value)).ToArray()))
            .ToArray();
    }
}

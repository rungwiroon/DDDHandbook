using Restaurant.Domain;

namespace Restaurant.Application;

public sealed class OrderCommandService
{
    private readonly IOrderRepository _orders;
    private readonly IDomainEventDispatcher _events;

    public OrderCommandService(IOrderRepository orders, IDomainEventDispatcher events)
    {
        _orders = orders;
        _events = events;
    }

    public void Create(CreateOrder command)
    {
        _orders.Add(Order.Create(command.OrderId, command.TableNumber));
    }

    public void AddItem(AddItemToOrder command)
    {
        var order = GetOrder(command.OrderId);
        order.AddItem(command.MenuItemId, command.ItemName, command.UnitPrice, command.Quantity);
        _orders.Save(order);
    }

    public void RemoveItem(RemoveOrderItem command)
    {
        var order = GetOrder(command.OrderId);
        order.RemoveItem(command.MenuItemId);
        _orders.Save(order);
    }

    public void SendToKitchen(SendOrderToKitchen command)
    {
        var order = GetOrder(command.OrderId);
        order.SendToKitchen();
        _orders.Save(order);
        foreach (var domainEvent in order.DequeueDomainEvents())
        {
            _events.Dispatch(domainEvent);
        }
    }

    private Order GetOrder(OrderId orderId) =>
        _orders.Find(orderId) ?? throw new OrderNotFoundException();

}

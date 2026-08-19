using Restaurant.Domain;

namespace Restaurant.Application;

public sealed class OrderCommandService
{
    private readonly IOrderRepository _orders;
    public OrderCommandService(IOrderRepository orders)
    {
        _orders = orders;
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
        _orders.Save(order, order.DomainEvents);
        order.DequeueDomainEvents();
    }

    private Order GetOrder(OrderId orderId) =>
        _orders.Find(orderId) ?? throw new OrderNotFoundException();

}

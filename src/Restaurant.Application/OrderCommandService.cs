using Restaurant.Domain;

namespace Restaurant.Application;

public sealed class OrderCommandService(IOrderRepository orders)
{
    public void Create(CreateOrder command)
    {
        orders.Add(Order.Create(command.OrderId, command.TableNumber));
    }

    public void AddItem(AddItemToOrder command)
    {
        var order = GetOrder(command.OrderId);
        order.AddItem(command.MenuItemId, command.ItemName, command.UnitPrice, command.Quantity);
        orders.Save(order);
    }

    public void RemoveItem(RemoveOrderItem command)
    {
        var order = GetOrder(command.OrderId);
        order.RemoveItem(command.MenuItemId);
        orders.Save(order);
    }

    public void SendToKitchen(SendOrderToKitchen command)
    {
        var order = GetOrder(command.OrderId);
        order.SendToKitchen();
        orders.Save(order);
    }

    private Order GetOrder(OrderId orderId) =>
        orders.Find(orderId) ?? throw new OrderNotFoundException();
}

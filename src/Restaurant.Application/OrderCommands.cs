using Restaurant.Domain;

namespace Restaurant.Application;

public sealed record CreateOrder(OrderId OrderId, TableNumber TableNumber);

public sealed record AddItemToOrder(
    OrderId OrderId,
    MenuItemId MenuItemId,
    string ItemName,
    Money UnitPrice,
    Quantity Quantity);

public sealed record RemoveOrderItem(OrderId OrderId, MenuItemId MenuItemId);

public sealed record SendOrderToKitchen(OrderId OrderId);

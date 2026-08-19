using Restaurant.Domain;

namespace Restaurant.Application;

public sealed record CreateOrder(OrderId OrderId, TableNumber TableNumber);

public sealed record AddItemToOrder(
    OrderId OrderId,
    MenuItemId MenuItemId,
    string ItemName,
    Money UnitPrice,
    Quantity Quantity,
    int? ExpectedVersion = null);

public sealed record RemoveOrderItem(OrderId OrderId, MenuItemId MenuItemId, int? ExpectedVersion = null);

public sealed record SendOrderToKitchen(OrderId OrderId, int? ExpectedVersion = null);

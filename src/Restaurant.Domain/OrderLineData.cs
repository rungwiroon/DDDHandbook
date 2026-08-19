namespace Restaurant.Domain;

public sealed record OrderLineData(
    MenuItemId MenuItemId,
    string ItemName,
    Money UnitPrice,
    Quantity Quantity);

namespace Restaurant.Domain;

public sealed class OrderLine
{
    internal OrderLine(MenuItemId menuItemId, string itemName, Money unitPrice, Quantity quantity)
    {
        MenuItemId = menuItemId;
        ItemName = itemName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public MenuItemId MenuItemId { get; }

    public string ItemName { get; }

    public Money UnitPrice { get; }

    public Quantity Quantity { get; private set; }

    public Money Subtotal => Money.From(UnitPrice.Value * Quantity.Value);

    internal void IncreaseQuantity(Quantity quantity) =>
        Quantity = Quantity.From(Quantity.Value + quantity.Value);
}

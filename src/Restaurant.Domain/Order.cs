namespace Restaurant.Domain;

public sealed class Order
{
    private const string NotDraftCode = "order.not-draft";
    private const string ItemNameRequiredCode = "order.item-name-required";
    private const string LineNotFoundCode = "order.line-not-found";
    private const string EmptyOrderCode = "order.empty";

    private readonly List<OrderLine> _lines = [];

    private Order(OrderId id, TableNumber tableNumber)
    {
        Id = id;
        TableNumber = tableNumber;
        Status = OrderStatus.Draft;
    }

    public OrderId Id { get; }

    public TableNumber TableNumber { get; }

    public OrderStatus Status { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    public Money Total => Money.From(_lines.Sum(line => line.Subtotal.Value));

    public static Order Create(OrderId id, TableNumber tableNumber) => new(id, tableNumber);

    public void AddItem(MenuItemId menuItemId, string itemName, Money unitPrice, Quantity quantity)
    {
        EnsureDraft();

        if (string.IsNullOrWhiteSpace(itemName))
        {
            throw new DomainRuleViolationException(ItemNameRequiredCode, "Order item name is required.");
        }

        var existingLine = _lines.SingleOrDefault(line => line.MenuItemId == menuItemId);
        if (existingLine is not null)
        {
            existingLine.IncreaseQuantity(quantity);
            return;
        }

        _lines.Add(new OrderLine(menuItemId, itemName, unitPrice, quantity));
    }

    public void RemoveItem(MenuItemId menuItemId)
    {
        EnsureDraft();

        var line = _lines.SingleOrDefault(existingLine => existingLine.MenuItemId == menuItemId);
        if (line is null)
        {
            throw new DomainRuleViolationException(LineNotFoundCode, "Order item was not found.");
        }

        _lines.Remove(line);
    }

    public void SendToKitchen()
    {
        EnsureDraft();

        if (_lines.Count == 0)
        {
            throw new DomainRuleViolationException(EmptyOrderCode, "An empty order cannot be sent to the kitchen.");
        }

        Status = OrderStatus.SentToKitchen;
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new DomainRuleViolationException(NotDraftCode, "Only draft orders can be changed.");
        }
    }
}

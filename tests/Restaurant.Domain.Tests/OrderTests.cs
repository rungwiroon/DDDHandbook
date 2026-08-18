using Restaurant.Domain;

namespace Restaurant.Domain.Tests;

public sealed class OrderTests
{
    [Fact]
    public void Create_starts_a_draft_order_for_the_table()
    {
        var id = OrderId.From(Guid.NewGuid());
        var table = TableNumber.From(12);

        var order = Order.Create(id, table);

        Assert.Equal(id, order.Id);
        Assert.Equal(table, order.TableNumber);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Empty(order.Lines);
        Assert.Equal(0m, order.Total.Value);
    }

    [Fact]
    public void Add_item_adds_a_line_with_snapshots_and_subtotal()
    {
        var order = CreateOrder();
        var itemId = MenuItemId.From(Guid.NewGuid());

        order.AddItem(itemId, "Pad Thai", Money.From(85m), Quantity.From(2));

        var line = Assert.Single(order.Lines);
        Assert.Equal(itemId, line.MenuItemId);
        Assert.Equal("Pad Thai", line.ItemName);
        Assert.Equal(85m, line.UnitPrice.Value);
        Assert.Equal(2, line.Quantity.Value);
        Assert.Equal(170m, line.Subtotal.Value);
        Assert.Equal(170m, order.Total.Value);
    }

    [Fact]
    public void Add_item_with_the_same_menu_item_merges_its_quantity()
    {
        var order = CreateOrder();
        var itemId = MenuItemId.From(Guid.NewGuid());

        order.AddItem(itemId, "Pad Thai", Money.From(85m), Quantity.From(1));
        order.AddItem(itemId, "Changed name", Money.From(99m), Quantity.From(2));

        var line = Assert.Single(order.Lines);
        Assert.Equal(3, line.Quantity.Value);
        Assert.Equal("Pad Thai", line.ItemName);
        Assert.Equal(85m, line.UnitPrice.Value);
        Assert.Equal(255m, order.Total.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Add_item_rejects_a_blank_item_name(string itemName)
    {
        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            CreateOrder().AddItem(MenuItemId.From(Guid.NewGuid()), itemName, Money.From(85m), Quantity.From(1)));

        Assert.Equal("order.item-name-required", exception.Code);
    }

    [Fact]
    public void Remove_item_removes_an_existing_line()
    {
        var order = CreateOrder();
        var itemId = MenuItemId.From(Guid.NewGuid());
        order.AddItem(itemId, "Pad Thai", Money.From(85m), Quantity.From(1));

        order.RemoveItem(itemId);

        Assert.Empty(order.Lines);
        Assert.Equal(0m, order.Total.Value);
    }

    [Fact]
    public void Remove_item_rejects_a_missing_line()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            CreateOrder().RemoveItem(MenuItemId.From(Guid.NewGuid())));

        Assert.Equal("order.line-not-found", exception.Code);
    }

    [Fact]
    public void Send_to_kitchen_rejects_an_empty_order()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(() => CreateOrder().SendToKitchen());

        Assert.Equal("order.empty", exception.Code);
    }

    [Fact]
    public void Send_to_kitchen_changes_the_order_status()
    {
        var order = CreateOrder();
        order.AddItem(MenuItemId.From(Guid.NewGuid()), "Pad Thai", Money.From(85m), Quantity.From(1));

        order.SendToKitchen();

        Assert.Equal(OrderStatus.SentToKitchen, order.Status);
    }

    [Fact]
    public void Send_to_kitchen_queues_one_immutable_event_with_order_facts()
    {
        var order = CreateOrder();
        var menuItemId = MenuItemId.From(Guid.NewGuid());
        order.AddItem(menuItemId, "Pad Thai", Money.From(85m), Quantity.From(2));

        order.SendToKitchen();

        var domainEvent = Assert.IsType<OrderSentToKitchen>(Assert.Single(order.DomainEvents));
        Assert.Equal(order.Id, domainEvent.OrderId);
        Assert.Equal(order.TableNumber, domainEvent.TableNumber);
        var line = Assert.Single(domainEvent.Lines);
        Assert.Equal(menuItemId, line.MenuItemId);
        Assert.Equal(2, line.Quantity.Value);
        Assert.Single(order.DequeueDomainEvents());
        Assert.Empty(order.DomainEvents);
    }

    [Fact]
    public void Empty_order_does_not_queue_a_domain_event()
    {
        var order = CreateOrder();

        Assert.Throws<DomainRuleViolationException>(order.SendToKitchen);

        Assert.Empty(order.DomainEvents);
    }

    [Fact]
    public void Sent_order_rejects_add_remove_and_send_operations()
    {
        var order = CreateOrder();
        order.AddItem(MenuItemId.From(Guid.NewGuid()), "Pad Thai", Money.From(85m), Quantity.From(1));
        order.SendToKitchen();

        Assert.Equal("order.not-draft", Assert.Throws<DomainRuleViolationException>(() =>
            order.AddItem(MenuItemId.From(Guid.NewGuid()), "Green Curry", Money.From(95m), Quantity.From(1))).Code);
        Assert.Equal("order.not-draft", Assert.Throws<DomainRuleViolationException>(() =>
            order.RemoveItem(MenuItemId.From(Guid.NewGuid()))).Code);
        Assert.Equal("order.not-draft", Assert.Throws<DomainRuleViolationException>(order.SendToKitchen).Code);
    }

    [Fact]
    public void Total_is_the_sum_of_all_line_subtotals()
    {
        var order = CreateOrder();
        order.AddItem(MenuItemId.From(Guid.NewGuid()), "Pad Thai", Money.From(85m), Quantity.From(2));
        order.AddItem(MenuItemId.From(Guid.NewGuid()), "Iced Tea", Money.From(30m), Quantity.From(3));

        Assert.Equal(260m, order.Total.Value);
    }

    private static Order CreateOrder() =>
        Order.Create(OrderId.From(Guid.NewGuid()), TableNumber.From(1));
}

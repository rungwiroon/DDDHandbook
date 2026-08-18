using Restaurant.Domain;
using Vogen;

namespace Restaurant.Domain.Tests;

public sealed class ValueObjectTests
{
    [Fact]
    public void Order_and_menu_item_ids_preserve_their_guid_values()
    {
        var orderId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();

        Assert.Equal(orderId, OrderId.From(orderId).Value);
        Assert.Equal(menuItemId, MenuItemId.From(menuItemId).Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Table_number_must_be_greater_than_zero(int value)
    {
        Assert.Throws<ValueObjectValidationException>(() => TableNumber.From(value));
    }

    [Fact]
    public void Money_cannot_be_negative()
    {
        Assert.Throws<ValueObjectValidationException>(() => Money.From(-0.01m));
    }

    [Fact]
    public void Money_can_be_zero()
    {
        Assert.Equal(0m, Money.From(0m).Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Quantity_must_be_greater_than_zero(int value)
    {
        Assert.Throws<ValueObjectValidationException>(() => Quantity.From(value));
    }
}

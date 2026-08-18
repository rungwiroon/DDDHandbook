using Restaurant.Application;
using Restaurant.Domain;

namespace Restaurant.Application.Tests;

public sealed class OrderCommandServiceTests
{
    [Fact]
    public void Add_item_loads_changes_and_saves_the_order()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderCommandService(repository);
        var orderId = OrderId.From(Guid.NewGuid());

        service.Create(new CreateOrder(orderId, TableNumber.From(1)));
        service.AddItem(new AddItemToOrder(
            orderId,
            MenuItemId.From(Guid.NewGuid()),
            "Pad Thai",
            Money.From(85m),
            Quantity.From(2)));

        var order = Assert.IsType<Order>(repository.Find(orderId));
        Assert.Equal(170m, order.Total.Value);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public void Command_for_an_unknown_order_is_rejected()
    {
        var service = new OrderCommandService(new FakeOrderRepository());

        Assert.Throws<OrderNotFoundException>(() =>
            service.SendToKitchen(new SendOrderToKitchen(OrderId.From(Guid.NewGuid()))));
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        private readonly Dictionary<OrderId, Order> _orders = [];

        public int SaveCount { get; private set; }

        public Order? Find(OrderId id) => _orders.GetValueOrDefault(id);

        public void Add(Order order) => _orders.Add(order.Id, order);

        public void Save(Order order) => SaveCount++;
    }
}

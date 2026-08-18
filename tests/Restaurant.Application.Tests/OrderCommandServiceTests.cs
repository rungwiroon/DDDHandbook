using Restaurant.Application;
using Restaurant.Domain;

namespace Restaurant.Application.Tests;

public sealed class OrderCommandServiceTests
{
    [Fact]
    public void Add_item_loads_changes_and_saves_the_order()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderCommandService(repository, new RecordingDispatcher(() => true));
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
        var service = new OrderCommandService(new FakeOrderRepository(), new RecordingDispatcher(() => true));

        Assert.Throws<OrderNotFoundException>(() =>
            service.SendToKitchen(new SendOrderToKitchen(OrderId.From(Guid.NewGuid()))));
    }

    [Fact]
    public void Send_dispatches_the_event_only_after_save()
    {
        var repository = new RecordingOrderRepository();
        var dispatcher = new RecordingDispatcher(() => repository.WasSaved);
        var service = new OrderCommandService(repository, dispatcher);
        var orderId = OrderId.From(Guid.NewGuid());
        service.Create(new CreateOrder(orderId, TableNumber.From(1)));
        service.AddItem(new AddItemToOrder(
            orderId,
            MenuItemId.From(Guid.NewGuid()),
            "Pad Thai",
            Money.From(85m),
            Quantity.From(1)));

        service.SendToKitchen(new SendOrderToKitchen(orderId));

        Assert.True(dispatcher.WasCalledAfterSave);
        Assert.Single(dispatcher.Events);
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        private readonly Dictionary<OrderId, Order> _orders = [];

        public int SaveCount { get; private set; }

        public Order? Find(OrderId id) => _orders.GetValueOrDefault(id);

        public void Add(Order order) => _orders.Add(order.Id, order);

        public void Save(Order order) => SaveCount++;
    }

    private sealed class RecordingOrderRepository : IOrderRepository
    {
        private readonly Dictionary<OrderId, Order> _orders = [];

        public bool WasSaved { get; private set; }

        public Order? Find(OrderId id) => _orders.GetValueOrDefault(id);

        public void Add(Order order) => _orders.Add(order.Id, order);

        public void Save(Order order) => WasSaved = true;
    }

    private sealed class RecordingDispatcher(Func<bool> wasSaved) : IDomainEventDispatcher
    {
        public List<IDomainEvent> Events { get; } = [];

        public bool WasCalledAfterSave { get; private set; }

        public void Dispatch(IDomainEvent domainEvent)
        {
            WasCalledAfterSave = wasSaved();
            Events.Add(domainEvent);
        }
    }
}

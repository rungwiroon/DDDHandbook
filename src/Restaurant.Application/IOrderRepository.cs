using Restaurant.Domain;

namespace Restaurant.Application;

public interface IOrderRepository
{
    Order? Find(OrderId id);

    void Add(Order order);

    void Save(Order order);
}

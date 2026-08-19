using Microsoft.EntityFrameworkCore;
using Restaurant.Application;
using Restaurant.Domain;

namespace Restaurant.Infrastructure;

public sealed class EfOrderRepository(RestaurantDbContext db) : IOrderRepository
{
    public Order? Find(OrderId id)
    {
        var record = db.Orders.AsNoTracking().Include(order => order.Lines).SingleOrDefault(order => order.Id == id.Value);
        return record is null ? null : ToDomain(record);
    }

    public void Add(Order order)
    {
        db.Orders.Add(ToRecord(order));
        db.SaveChanges();
    }

    public void Save(Order order)
    {
        var record = db.Orders.Include(existing => existing.Lines).SingleOrDefault(existing => existing.Id == order.Id.Value);
        if (record is null)
        {
            db.Orders.Add(ToRecord(order));
        }
        else
        {
            record.TableNumber = order.TableNumber.Value;
            record.Status = (int)order.Status;
            db.RemoveRange(record.Lines);
            record.Lines = ToRecord(order).Lines;
        }
        db.SaveChanges();
    }

    private static Order ToDomain(OrderRecord record) => Order.Rehydrate(
        OrderId.From(record.Id),
        TableNumber.From(record.TableNumber),
        (OrderStatus)record.Status,
        record.Lines.Select(line => new OrderLineData(
            MenuItemId.From(line.MenuItemId),
            line.ItemName,
            Money.From(line.UnitPrice),
            Quantity.From(line.Quantity))));

    private static OrderRecord ToRecord(Order order) => new()
    {
        Id = order.Id.Value,
        TableNumber = order.TableNumber.Value,
        Status = (int)order.Status,
        Lines = order.Lines.Select(line => new OrderLineRecord
        {
            OrderId = order.Id.Value,
            MenuItemId = line.MenuItemId.Value,
            ItemName = line.ItemName,
            UnitPrice = line.UnitPrice.Value,
            Quantity = line.Quantity.Value,
        }).ToList(),
    };
}

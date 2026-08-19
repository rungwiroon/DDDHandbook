using Microsoft.EntityFrameworkCore;
using Restaurant.Application;
using Restaurant.Domain;
using System.Text.Json;

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

    public void Save(Order order, IReadOnlyCollection<IDomainEvent>? events = null)
    {
        using var transaction = db.Database.BeginTransaction();
        var record = db.Orders.Include(existing => existing.Lines).SingleOrDefault(existing => existing.Id == order.Id.Value);
        if (record is null)
        {
            db.Orders.Add(ToRecord(order));
        }
        else
        {
            record.TableNumber = order.TableNumber.Value;
            record.Status = (int)order.Status;
            db.Entry(record).Property(existing => existing.Version).OriginalValue = order.Version;
            record.Version = order.Version + 1;
            db.RemoveRange(record.Lines);
            record.Lines = ToRecord(order).Lines;
        }
        foreach (var domainEvent in events ?? [])
        {
            if (domainEvent is OrderSentToKitchen sent)
            {
                db.Outbox.Add(new OutboxRecord
                {
                    Id = Guid.NewGuid(),
                    Type = nameof(OrderSentToKitchen),
                    Payload = JsonSerializer.Serialize(new OrderSentPayload(
                        sent.OrderId.Value,
                        sent.TableNumber.Value,
                        [.. sent.Lines.Select(line => new OrderLinePayload(line.MenuItemId.Value, line.ItemName, line.Quantity.Value))])),
                    OccurredUtc = DateTime.UtcNow,
                });
            }
        }
        try
        {
            db.SaveChanges();
            transaction.Commit();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new OrderConcurrencyException();
        }
    }

    private static Order ToDomain(OrderRecord record) => Order.Rehydrate(
        OrderId.From(record.Id),
        TableNumber.From(record.TableNumber),
        (OrderStatus)record.Status,
        record.Lines.Select(line => new OrderLineData(
            MenuItemId.From(line.MenuItemId),
            line.ItemName,
            Money.From(line.UnitPrice),
            Quantity.From(line.Quantity))),
        record.Version);

    private static OrderRecord ToRecord(Order order) => new()
    {
        Id = order.Id.Value,
        TableNumber = order.TableNumber.Value,
        Status = (int)order.Status,
        Version = order.Version,
        Lines = order.Lines.Select(line => new OrderLineRecord
        {
            OrderId = order.Id.Value,
            MenuItemId = line.MenuItemId.Value,
            ItemName = line.ItemName,
            UnitPrice = line.UnitPrice.Value,
            Quantity = line.Quantity.Value,
        }).ToList(),
    };

    private sealed record OrderLinePayload(Guid MenuItemId, string ItemName, int Quantity);

    private sealed record OrderSentPayload(Guid OrderId, int TableNumber, OrderLinePayload[] Lines);
}

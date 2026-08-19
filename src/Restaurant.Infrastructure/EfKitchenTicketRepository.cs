using Microsoft.EntityFrameworkCore;
using Restaurant.Application;
using Restaurant.Domain;

namespace Restaurant.Infrastructure;

public sealed class EfKitchenTicketRepository(RestaurantDbContext db) : IKitchenTicketRepository
{
    public KitchenTicket? Find(OrderId orderId)
    {
        var record = db.KitchenTickets.AsNoTracking().Include(ticket => ticket.Lines).SingleOrDefault(ticket => ticket.OrderId == orderId.Value);
        return record is null ? null : ToDomain(record);
    }

    public IReadOnlyCollection<KitchenTicket> All() => db.KitchenTickets.AsNoTracking().Include(ticket => ticket.Lines)
        .AsEnumerable().Select(ToDomain).ToArray();

    public void Add(KitchenTicket ticket) => db.KitchenTickets.Add(ToRecord(ticket));

    public void Save(KitchenTicket ticket) => db.SaveChanges();

    private static KitchenTicket ToDomain(KitchenTicketRecord record) => KitchenTicket.Rehydrate(
        OrderId.From(record.OrderId),
        TableNumber.From(record.TableNumber),
        record.Lines.Select(line => new OrderLineSnapshot(
            MenuItemId.From(line.MenuItemId),
            line.ItemName,
            Quantity.From(line.Quantity))));

    private static KitchenTicketRecord ToRecord(KitchenTicket ticket) => new()
    {
        OrderId = ticket.OrderId.Value,
        TableNumber = ticket.TableNumber.Value,
        Lines = ticket.Lines.Select(line => new KitchenTicketLineRecord
        {
            OrderId = ticket.OrderId.Value,
            MenuItemId = line.MenuItemId.Value,
            ItemName = line.ItemName,
            Quantity = line.Quantity.Value,
        }).ToList(),
    };
}

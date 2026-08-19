using Microsoft.EntityFrameworkCore;

namespace Restaurant.Infrastructure;

public sealed class RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : DbContext(options)
{
    public DbSet<OrderRecord> Orders => Set<OrderRecord>();

    public DbSet<KitchenTicketRecord> KitchenTickets => Set<KitchenTicketRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderRecord>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.Property(order => order.TableNumber).IsRequired();
            entity.Property(order => order.Status).IsRequired();
            entity.HasMany(order => order.Lines)
                .WithOne()
                .HasForeignKey(line => line.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderLineRecord>(entity =>
        {
            entity.HasKey(line => new { line.OrderId, line.MenuItemId });
            entity.Property(line => line.ItemName).IsRequired();
            entity.Property(line => line.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<KitchenTicketRecord>(entity =>
        {
            entity.HasKey(ticket => ticket.OrderId);
            entity.Property(ticket => ticket.TableNumber).IsRequired();
            entity.HasMany(ticket => ticket.Lines)
                .WithOne()
                .HasForeignKey(line => line.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KitchenTicketLineRecord>(entity =>
        {
            entity.HasKey(line => new { line.OrderId, line.MenuItemId });
            entity.Property(line => line.ItemName).IsRequired();
        });
    }
}

public sealed class OrderRecord
{
    public Guid Id { get; set; }
    public int TableNumber { get; set; }
    public int Status { get; set; }
    public List<OrderLineRecord> Lines { get; set; } = [];
}

public sealed class OrderLineRecord
{
    public Guid OrderId { get; set; }
    public Guid MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public sealed class KitchenTicketRecord
{
    public Guid OrderId { get; set; }
    public int TableNumber { get; set; }
    public List<KitchenTicketLineRecord> Lines { get; set; } = [];
}

public sealed class KitchenTicketLineRecord
{
    public Guid OrderId { get; set; }
    public Guid MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

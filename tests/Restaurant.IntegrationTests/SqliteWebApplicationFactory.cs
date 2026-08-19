using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Restaurant.Application;
using Restaurant.Infrastructure;

namespace Restaurant.IntegrationTests;

public sealed class SqliteWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public IReadOnlyCollection<Restaurant.Domain.KitchenTicket> ReadTickets()
    {
        using var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IKitchenTicketRepository>().All();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            _connection.Open();
            services.RemoveAll<IDbContextOptionsConfiguration<RestaurantDbContext>>();
            services.RemoveAll<DbContextOptions<RestaurantDbContext>>();
            services.RemoveAll<RestaurantDbContext>();
            services.RemoveAll<IOrderRepository>();
            services.RemoveAll<IKitchenTicketRepository>();
            services.AddSingleton(_connection);
            services.AddDbContext<RestaurantDbContext>(options => options.UseSqlite(_connection));
            services.AddScoped<IOrderRepository, EfOrderRepository>();
            services.AddScoped<IKitchenTicketRepository, EfKitchenTicketRepository>();

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<RestaurantDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }
        base.Dispose(disposing);
    }
}

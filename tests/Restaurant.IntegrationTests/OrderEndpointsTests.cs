using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Application;

namespace Restaurant.IntegrationTests;

public sealed class OrderEndpointsTests(SqliteWebApplicationFactory factory) : IClassFixture<SqliteWebApplicationFactory>
{
    [Fact]
    public async Task Get_health_returns_ok()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"status\":\"ok\"}", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_order_returns_created_with_location()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/orders", new { tableNumber = 12 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/orders/", response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task Add_item_to_a_draft_order_returns_no_content()
    {
        using var client = factory.CreateClient();
        var orderId = await CreateOrder(client);

        var response = await AddItem(client, orderId, "Pad Thai");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Removing_an_existing_item_returns_no_content()
    {
        using var client = factory.CreateClient();
        var orderId = await CreateOrder(client);
        var menuItemId = Guid.NewGuid();
        await client.PostAsJsonAsync($"/orders/{orderId}/items", new
        {
            menuItemId,
            itemName = "Pad Thai",
            unitPrice = 85.00m,
            quantity = 1,
        });

        var response = await client.DeleteAsync($"/orders/{orderId}/items/{menuItemId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Sending_an_empty_order_returns_business_error()
    {
        using var client = factory.CreateClient();
        var orderId = await CreateOrder(client);

        var response = await client.PostAsync($"/orders/{orderId}/send-to-kitchen", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("order.empty", await ReadErrorCode(response));
    }

    [Fact]
    public async Task Adding_to_a_sent_order_returns_business_error()
    {
        using var client = factory.CreateClient();
        var orderId = await CreateOrder(client);
        await AddItem(client, orderId, "Pad Thai");
        await client.PostAsync($"/orders/{orderId}/send-to-kitchen", null);

        var response = await AddItem(client, orderId, "Iced Tea");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("order.not-draft", await ReadErrorCode(response));
    }

    [Fact]
    public async Task Sending_a_populated_order_creates_exactly_one_kitchen_ticket()
    {
        using var client = factory.CreateClient();
        var orderId = await CreateOrder(client);
        await AddItem(client, orderId, "Pad Thai");

        var response = await client.PostAsync($"/orders/{orderId}/send-to-kitchen", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var tickets = factory.ReadTickets();
        var ticket = Assert.Single(tickets, ticket => ticket.OrderId.Value == orderId);
        Assert.Single(ticket.Lines);
    }

    [Fact]
    public async Task Sending_an_empty_order_creates_no_kitchen_ticket()
    {
        using var client = factory.CreateClient();
        var orderId = await CreateOrder(client);
        var ticketsBefore = factory.ReadTickets().Count;

        var response = await client.PostAsync($"/orders/{orderId}/send-to-kitchen", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ticketsBefore, factory.ReadTickets().Count);
    }

    [Fact]
    public async Task Command_for_an_unknown_order_returns_not_found()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/orders/{Guid.NewGuid()}/send-to-kitchen", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Order_detail_returns_a_read_dto_without_domain_object_shape()
    {
        using var client = factory.CreateClient();
        var orderId = await CreateOrder(client);
        await AddItem(client, orderId, "Pad Thai");

        var detail = await client.GetFromJsonAsync<JsonElement>($"/orders/{orderId}");

        Assert.Equal(orderId, detail.GetProperty("orderId").GetGuid());
        Assert.Equal(1, detail.GetProperty("tableNumber").GetInt32());
        Assert.Equal("Draft", detail.GetProperty("status").GetString());
        Assert.Equal("Pad Thai", detail.GetProperty("lines")[0].GetProperty("itemName").GetString());
        Assert.Equal(85m, detail.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task Unknown_order_detail_returns_not_found()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Kitchen_board_returns_ticket_read_models()
    {
        using var client = factory.CreateClient();
        var orderId = await CreateOrder(client);
        await AddItem(client, orderId, "Pad Thai");
        await client.PostAsync($"/orders/{orderId}/send-to-kitchen", null);

        var board = await client.GetFromJsonAsync<JsonElement>("/kitchen-board");
        var ticket = board.EnumerateArray().Single(ticket => ticket.GetProperty("orderId").GetGuid() == orderId);

        Assert.Equal(1, ticket.GetProperty("tableNumber").GetInt32());
        Assert.Equal("Pad Thai", ticket.GetProperty("lines")[0].GetProperty("itemName").GetString());
        Assert.Equal(1, ticket.GetProperty("lines")[0].GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task Order_and_ticket_are_readable_after_a_new_client_scope()
    {
        Guid orderId;
        using (var firstClient = factory.CreateClient())
        {
            orderId = await CreateOrder(firstClient);
            await AddItem(firstClient, orderId, "Pad Thai");
            Assert.Equal(HttpStatusCode.NoContent,
                (await firstClient.PostAsync($"/orders/{orderId}/send-to-kitchen", null)).StatusCode);
        }

        using var secondClient = factory.CreateClient();
        var detail = await secondClient.GetFromJsonAsync<JsonElement>($"/orders/{orderId}");
        var board = await secondClient.GetFromJsonAsync<JsonElement>("/kitchen-board");

        Assert.Equal("SentToKitchen", detail.GetProperty("status").GetString());
        Assert.Contains(board.EnumerateArray(), ticket => ticket.GetProperty("orderId").GetGuid() == orderId);
    }

    [Fact]
    public async Task Send_persists_pending_outbox_then_processor_marks_it_after_ticket_creation()
    {
        using var client = factory.CreateClient();
        var orderId = await CreateOrder(client);
        await AddItem(client, orderId, "Pad Thai");

        using var scope = factory.Services.CreateScope();
        var commands = scope.ServiceProvider.GetRequiredService<OrderCommandService>();
        commands.SendToKitchen(new SendOrderToKitchen(Restaurant.Domain.OrderId.From(orderId)));
        var context = scope.ServiceProvider.GetRequiredService<Restaurant.Infrastructure.RestaurantDbContext>();

        var pending = Assert.Single(context.Outbox.Where(message => message.ProcessedUtc == null));
        Assert.Null(scope.ServiceProvider.GetRequiredService<IKitchenTicketRepository>().Find(Restaurant.Domain.OrderId.From(orderId)));

        scope.ServiceProvider.GetRequiredService<IOutboxProcessor>().ProcessPending();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IKitchenTicketRepository>().Find(Restaurant.Domain.OrderId.From(orderId)));
        Assert.NotNull(context.Outbox.Single(message => message.Id == pending.Id).ProcessedUtc);
    }

    [Fact]
    public async Task Duplicate_outbox_delivery_is_idempotent_by_order_id()
    {
        using var client = factory.CreateClient();
        var orderId = await CreateOrder(client);
        await AddItem(client, orderId, "Pad Thai");

        using var scope = factory.Services.CreateScope();
        var commands = scope.ServiceProvider.GetRequiredService<OrderCommandService>();
        commands.SendToKitchen(new SendOrderToKitchen(Restaurant.Domain.OrderId.From(orderId)));
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
        processor.ProcessPending();
        processor.ProcessPending();

        Assert.Single(scope.ServiceProvider.GetRequiredService<IKitchenTicketRepository>().All(), ticket => ticket.OrderId.Value == orderId);
    }

    [Fact]
    public async Task Failed_outbox_delivery_stays_pending_for_retry()
    {
        using var client = factory.CreateClient();
        var orderId = await CreateOrder(client);
        await AddItem(client, orderId, "Pad Thai");

        using var scope = factory.Services.CreateScope();
        var order = Restaurant.Domain.OrderId.From(orderId);
        scope.ServiceProvider.GetRequiredService<OrderCommandService>().SendToKitchen(new SendOrderToKitchen(order));
        var context = scope.ServiceProvider.GetRequiredService<Restaurant.Infrastructure.RestaurantDbContext>();
        var failing = new Restaurant.Infrastructure.EfOutboxProcessor(context, new ThrowingDispatcher());

        Assert.Throws<InvalidOperationException>(failing.ProcessPending);
        Assert.Single(context.Outbox.Where(message => message.ProcessedUtc == null));

        scope.ServiceProvider.GetRequiredService<IOutboxProcessor>().ProcessPending();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IKitchenTicketRepository>().Find(order));
    }

    [Fact]
    public async Task Concurrent_order_updates_return_a_stable_concurrency_error()
    {
        using var client = factory.CreateClient();
        var orderId = await CreateOrder(client);
        await AddItem(client, orderId, "Pad Thai");

        using var first = factory.Services.CreateScope();
        using var second = factory.Services.CreateScope();
        var firstRepository = first.ServiceProvider.GetRequiredService<IOrderRepository>();
        var secondRepository = second.ServiceProvider.GetRequiredService<IOrderRepository>();
        var firstOrder = firstRepository.Find(Restaurant.Domain.OrderId.From(orderId));
        var secondOrder = secondRepository.Find(Restaurant.Domain.OrderId.From(orderId));
        Assert.NotNull(firstOrder);
        Assert.NotNull(secondOrder);
        firstOrder.AddItem(Restaurant.Domain.MenuItemId.From(Guid.NewGuid()), "Iced Tea", Restaurant.Domain.Money.From(30), Restaurant.Domain.Quantity.From(1));
        secondOrder.AddItem(Restaurant.Domain.MenuItemId.From(Guid.NewGuid()), "Green Curry", Restaurant.Domain.Money.From(95), Restaurant.Domain.Quantity.From(1));
        firstRepository.Save(firstOrder);

        Assert.Throws<OrderConcurrencyException>(() => secondRepository.Save(secondOrder));
    }

    private static async Task<Guid> CreateOrder(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/orders", new { tableNumber = 1 });
        var location = Assert.IsType<Uri>(response.Headers.Location);

        return Guid.Parse(location.OriginalString.Split('/')[^1]);
    }

    private static Task<HttpResponseMessage> AddItem(HttpClient client, Guid orderId, string itemName) =>
        client.PostAsJsonAsync($"/orders/{orderId}/items", new
        {
            menuItemId = Guid.NewGuid(),
            itemName,
            unitPrice = 85.00m,
            quantity = 1,
        });

    private static async Task<string?> ReadErrorCode(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("code").GetString();
    }

    private sealed class ThrowingDispatcher : IDomainEventDispatcher
    {
        public void Dispatch(Restaurant.Domain.IDomainEvent domainEvent) => throw new InvalidOperationException("simulated dispatch failure");
    }
}

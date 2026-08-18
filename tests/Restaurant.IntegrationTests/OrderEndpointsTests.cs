using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Application;

namespace Restaurant.IntegrationTests;

public sealed class OrderEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
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
        var tickets = factory.Services.GetRequiredService<IKitchenTicketRepository>().All();
        var ticket = Assert.Single(tickets, ticket => ticket.OrderId.Value == orderId);
        Assert.Single(ticket.Lines);
    }

    [Fact]
    public async Task Sending_an_empty_order_creates_no_kitchen_ticket()
    {
        using var client = factory.CreateClient();
        var orderId = await CreateOrder(client);
        var ticketsBefore = factory.Services.GetRequiredService<IKitchenTicketRepository>().All().Count;

        var response = await client.PostAsync($"/orders/{orderId}/send-to-kitchen", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ticketsBefore, factory.Services.GetRequiredService<IKitchenTicketRepository>().All().Count);
    }

    [Fact]
    public async Task Command_for_an_unknown_order_returns_not_found()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/orders/{Guid.NewGuid()}/send-to-kitchen", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
}

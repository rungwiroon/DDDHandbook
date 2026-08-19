using System.Net;
using System.Net.Http.Json;

namespace Restaurant.IntegrationTests;

public sealed class ReliabilityEndpointTests(FailingOutboxFactory factory) : IClassFixture<FailingOutboxFactory>
{
    [Fact]
    public async Task Send_returns_success_when_immediate_outbox_processing_fails()
    {
        factory.FailOutboxProcessing = true;
        using var client = factory.CreateClient();
        var create = await client.PostAsJsonAsync("/orders", new { tableNumber = 4 });
        var orderId = Guid.Parse(create.Headers.Location!.OriginalString.Split('/')[^1]);
        await client.PostAsJsonAsync($"/orders/{orderId}/items", new
        {
            menuItemId = Guid.NewGuid(), itemName = "Pad Thai", unitPrice = 85m, quantity = 1,
        });

        var response = await client.PostAsJsonAsync($"/orders/{orderId}/send-to-kitchen", new { version = 1 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, factory.ReadPendingOutbox());
    }
}

public sealed class FailingOutboxFactory : SqliteWebApplicationFactory;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Restaurant.Application;
using Restaurant.Domain;
using Restaurant.Infrastructure;
using Vogen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RestaurantDbContext>(options => options.UseSqlite(
    builder.Configuration.GetConnectionString("Restaurant")
    ?? throw new InvalidOperationException("ConnectionStrings:Restaurant is required.")));
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<IKitchenTicketRepository, EfKitchenTicketRepository>();
builder.Services.AddScoped<IDomainEventDispatcher, OrderSentToKitchenDispatcher>();
builder.Services.AddScoped<OrderCommandService>();
builder.Services.AddScoped<OrderQueryService>();
builder.Services.AddScoped<KitchenBoardQueryService>();

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error
        ?? new InvalidOperationException("The exception handler was invoked without an exception.");

    await ToProblem(error).ExecuteAsync(context);
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

var orders = app.MapGroup("/orders");

orders.MapPost("", (CreateOrderRequest request, OrderCommandService commands) =>
{
    var orderId = OrderId.From(Guid.NewGuid());
    commands.Create(new CreateOrder(orderId, TableNumber.From(request.TableNumber)));

    return Results.Created($"/orders/{orderId.Value}", new { orderId = orderId.Value, status = OrderStatus.Draft.ToString() });
});

orders.MapPost("/{orderId:guid}/items", (Guid orderId, AddItemRequest request, OrderCommandService commands) =>
{
    commands.AddItem(new AddItemToOrder(
        OrderId.From(orderId),
        MenuItemId.From(request.MenuItemId),
        request.ItemName,
        Money.From(request.UnitPrice),
        Quantity.From(request.Quantity)));

    return Results.NoContent();
});

orders.MapDelete("/{orderId:guid}/items/{menuItemId:guid}", (Guid orderId, Guid menuItemId, OrderCommandService commands) =>
{
    commands.RemoveItem(new RemoveOrderItem(OrderId.From(orderId), MenuItemId.From(menuItemId)));

    return Results.NoContent();
});

orders.MapPost("/{orderId:guid}/send-to-kitchen", (Guid orderId, OrderCommandService commands) =>
{
    commands.SendToKitchen(new SendOrderToKitchen(OrderId.From(orderId)));

    return Results.NoContent();
});

orders.MapGet("/{orderId:guid}", (Guid orderId, OrderQueryService queries) =>
    Results.Ok(queries.Get(OrderId.From(orderId))));

app.MapGet("/kitchen-board", (KitchenBoardQueryService queries) => Results.Ok(queries.Get()));

app.Run();

static IResult ToProblem(Exception error) => error switch
{
    OrderNotFoundException => Results.Problem(
        detail: error.Message,
        statusCode: StatusCodes.Status404NotFound,
        title: "Order was not found",
        type: "https://restaurant.example/problems/order-not-found"),
    DomainRuleViolationException rule => Results.Problem(
        detail: rule.Message,
        statusCode: rule.Code == "order.item-name-required"
            ? StatusCodes.Status422UnprocessableEntity
            : StatusCodes.Status409Conflict,
        title: rule.Code == "order.item-name-required" ? "Order item is invalid" : "Order cannot be changed",
        type: $"https://restaurant.example/problems/{rule.Code.Replace('.', '-')}",
        extensions: new Dictionary<string, object?> { ["code"] = rule.Code }),
    ValueObjectValidationException => Results.Problem(
        detail: error.Message,
        statusCode: StatusCodes.Status400BadRequest,
        title: "Request is invalid",
        type: "https://restaurant.example/problems/invalid-request"),
    _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError),
};

public sealed record CreateOrderRequest(int TableNumber);

public sealed record AddItemRequest(Guid MenuItemId, string ItemName, decimal UnitPrice, int Quantity);

public partial class Program;

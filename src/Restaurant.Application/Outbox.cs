using Restaurant.Domain;

namespace Restaurant.Application;

public sealed record OutboxMessage(Guid Id, string Type, string Payload);

public interface IOutboxProcessor
{
    void ProcessPending();
}

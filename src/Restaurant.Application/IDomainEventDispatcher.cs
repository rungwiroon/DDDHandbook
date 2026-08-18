using Restaurant.Domain;

namespace Restaurant.Application;

public interface IDomainEventDispatcher
{
    void Dispatch(IDomainEvent domainEvent);
}

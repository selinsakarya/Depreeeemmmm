using Depreeeemmmm.Data.Entities;

namespace Depreeeemmmm.Factories;

public interface IOutboxMessageFactory
{
    OutboxMessage From<T>(T @event, DateTime now, string queueName = null) where T : class, new();
}
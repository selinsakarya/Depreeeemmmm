using System.Text.Json;
using Depreeeemmmm.Data.Entities;
using Depreeeemmmm.Data.Enums;

namespace Depreeeemmmm.Factories;

public class OutboxMessageFactory : IOutboxMessageFactory
{
    public OutboxMessage From<T>(T @event, DateTime now, string queueName = null) where T : class, new()
    {
        string data = JsonSerializer.Serialize(@event);

        string type = @event.GetType().FullName;

        OutboxMessage outboxMessage = new OutboxMessage
        {
            Data = data,
            OccurredAt = now,
            Status = OutboxMessageStatus.New,
            Type = type,
            QueueName = queueName
        };

        return outboxMessage;
    }
}
using Depreeeemmmm.Data.Enums;

namespace Depreeeemmmm.Data.Entities;

public class OutboxMessage
{
    public long Id { get; set; }

    public string Data { get; set; }

    public DateTime OccurredOn { get; set; }

    public OutboxMessageStatus Status { get; set; }

    public DateTime? ProcessedDate { get; set; }

    public string Type { get; set; }

    public string QueueName { get; set; }

    public string RoutingKey { get; set; }

    public OutboxMessage() { }

    public OutboxMessage(DateTime occurredOn, string type, string data)
    {
        OccurredOn = occurredOn;
        Type = type;
        Data = data;
        Status = OutboxMessageStatus.New;
    }
}
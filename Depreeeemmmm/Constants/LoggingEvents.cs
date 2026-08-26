namespace Depreeeemmmm.Constants;

public static class LoggingEvents
{
    public static readonly EventId OutboxMessagePublishFailed = new EventId(1001, nameof(OutboxMessagePublishFailed));

    public const string OutboxJobsPayload = "@OutboxJobsPayload";
}
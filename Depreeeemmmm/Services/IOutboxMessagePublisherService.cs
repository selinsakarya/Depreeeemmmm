namespace Depreeeemmmm.Services;

public interface IOutboxMessagePublisherService
{
    Task Publish(CancellationToken cancellationToken);
}
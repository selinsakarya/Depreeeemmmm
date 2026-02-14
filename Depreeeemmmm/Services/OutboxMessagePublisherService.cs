using Dapper;
using Depreeeemmmm.Constants;
using Depreeeemmmm.Data.Entities;
using MassTransit;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Polly;
using Polly.Fallback;
using Polly.Retry;

namespace Depreeeemmmm.Services;

public class OutboxMessagePublisherService : IOutboxMessagePublisherService
{
    private readonly IConfiguration _configuration;
    private readonly IBusControl _busControl;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly ILogger<OutboxMessagePublisherService> _logger;

    public OutboxMessagePublisherService(
        IConfiguration configuration,
        IBusControl busControl,
        ISendEndpointProvider sendEndpointProvider,
        ILogger<OutboxMessagePublisherService> logger
        )
    {
        _configuration = configuration;
        _sendEndpointProvider = sendEndpointProvider;
        _logger = logger;
        _busControl = busControl;
    }

    public async Task Publish(CancellationToken message)
    {
        IEnumerable<OutboxMessage> outboxMessages = await GetOutboxMessages();

        Parallel.ForEach(outboxMessages, cancellationToken =>
        {
            FallbackPolicy fallbackPolicy = Policy
                .Handle<Exception>()
                .Fallback(_ =>
                {
                    _logger.LogError(LoggingEvents.OutboxMessagePublishFailed, LoggingEvents.OrderOutboxJobsLogPayload, new
                    {
                        MessageId = cancellationToken.Id,
                        MessageType = cancellationToken.Type
                    });
                });

            RetryPolicy retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetry(5,
                    retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt))
                    , (exception, _, retryCount, _) =>
                    {
                        _logger.LogWarning(LoggingEvents.OutboxMessagePublishFailed, exception, LoggingEvents.OrderOutboxJobsLogPayload, new
                        {
                            RetryCount = retryCount,
                            MessageId = cancellationToken.Id,
                            MessageType = cancellationToken.Type
                        });
                    });

            fallbackPolicy
                .Wrap(retryPolicy)
                .Execute(() => ProcessMessage(cancellationToken).Wait());
        });
    }

    private async Task ProcessMessage(OutboxMessage outboxMessage)
    {
        await PublishOrSendMessage(outboxMessage);
        
        await UpdateMessageStatusAsPublished(outboxMessage);
    }
    
    private async Task PublishOrSendMessage(OutboxMessage outboxMessage)
    {
        Type? type = Type.GetType(outboxMessage.Type);

        if (type is not null)
        {
            object? domainEvent = JsonConvert.DeserializeObject(outboxMessage.Data, type);

            if (domainEvent is not null)
            {
                if (string.IsNullOrWhiteSpace(outboxMessage.QueueName))
                {
                    await Publish(domainEvent, outboxMessage.RoutingKey);
                }
                else
                {
                    await Send(domainEvent, outboxMessage.QueueName);
                }
            }
        }
    }

    private async Task Publish(Object domainEvent, string? routingKey)
    {
        await _busControl.Publish(domainEvent, p =>
        {
            if (!string.IsNullOrWhiteSpace(routingKey))
            {
                p.SetRoutingKey(routingKey);
            }
        });
    }
    
    private async Task Send(object domainEvent, string queueName)
    {
        ISendEndpoint sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{queueName}"));
        
        await sendEndpoint.Send(domainEvent);
    }

    private async Task UpdateMessageStatusAsPublished(OutboxMessage outboxMessage)
    {
        DateTime now = DateTime.UtcNow;
        
        using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DepremDbConnectionString")))
        {
            await conn.ExecuteAsync(@"UPDATE OutboxMessages 
                                      SET Status = 3,
                                          ProcessedDate=@ProcessedDate
                                       WHERE Id = @Id", 
                new { outboxMessage.Id, ProcessedDate = now});
        }
    }

    private async Task<IEnumerable<OutboxMessage>> GetOutboxMessages()
    {
        using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DepremDbConnectionString")))
        {
            DateTime now = DateTime.UtcNow;

            DateTime before = now.AddMinutes(-1);

            return await conn.QueryAsync<OutboxMessage>(@"UPDATE TOP (42) OutboxMessages
                                                            SET Status = 2,
                                                                ProcessedDate = @ProcessedDate
                                                            OUTPUT Inserted.Id,
                                                                   Inserted.[Type],
                                                                   Inserted.[Data],
                                                                   Inserted.ProcessedDate,
                                                                   Inserted.QueueName,
                                                                   Inserted.RoutingKey
                                                            WHERE Status = 1
                                                               OR (Status = 2 AND ProcessedDate <= @BeforeProcessedDate)
                                                            ",
                new { ProcessedDate = now, BeforeProcessedDate = before }
            );
        }
    }
}
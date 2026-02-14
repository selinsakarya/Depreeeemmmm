using Depreeeemmmm.Services;
using Microsoft.Data.SqlClient;

namespace Depreeeemmmm.Jobs;

public class OutboxMessagePublisherHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxMessagePublisherHostedService> _logger;

    private static readonly Random Random = new Random();

    public OutboxMessagePublisherHostedService(
        IServiceProvider serviceProvider,
        ILogger<OutboxMessagePublisherHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested is false)
        {
            await PublishMessages(cancellationToken);

            TimeSpan delay = TimeSpan.FromMilliseconds(Random.Next(100, 200));
            
            await Task.Delay(delay, cancellationToken);
        }
    }

    private async Task PublishMessages(CancellationToken stoppingToken)
    {
        using (IServiceScope scope = _serviceProvider.CreateScope())
        {
            IOutboxMessagePublisherService outboxMessagePublisherService = scope.ServiceProvider.GetRequiredService<IOutboxMessagePublisherService>();

            try
            {
                await outboxMessagePublisherService.Publish(stoppingToken);
            }
            catch (SqlException sqlException) when ((sqlException.Message.Contains("deadlocked on lock resources with another process")))
            {
                _logger.LogWarning(sqlException, sqlException.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
    }
}
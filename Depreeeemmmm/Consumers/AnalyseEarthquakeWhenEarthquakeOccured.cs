using Events;
using MassTransit;

namespace Depreeeemmmm.Consumers;

public class AnalyseEarthquakeWhenEarthquakeOccured : IConsumer<EarthquakeOccurred>
{
    private readonly ILogger<AnalyseEarthquakeWhenEarthquakeOccured> _logger;

    public AnalyseEarthquakeWhenEarthquakeOccured(
        ILogger<AnalyseEarthquakeWhenEarthquakeOccured> logger)
    {
        _logger = logger;
    }
    
    public Task Consume(ConsumeContext<EarthquakeOccurred> context)
    {
        EarthquakeOccurred earthquakeOccurredEvent = context.Message;
        
        _logger.LogInformation("AnalyseEarthquakeWhenEarthquakeOccured is started. EarthquakeSecondaryUniqueId: {EarthquakeSecondaryUniqueId}", earthquakeOccurredEvent.EarthquakeSecondaryUniqueId);

        _logger.LogInformation("AnalyseEarthquakeWhenEarthquakeOccured is finished. EarthquakeSecondaryUniqueId: {EarthquakeSecondaryUniqueId}", earthquakeOccurredEvent.EarthquakeSecondaryUniqueId);
        
        return Task.CompletedTask;
    }
}
using Depreeeemmmm.Data;
using Depreeeemmmm.Data.Entities;
using Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Depreeeemmmm.Consumers;

public class NotifySelinWhenEarthquakeWhenEarthquakeOccured : IConsumer<EarthquakeOccurred>
{
    private readonly ILogger<NotifySelinWhenEarthquakeWhenEarthquakeOccured> _logger;
    private readonly DepremDbContext _depremDbContext;

    public NotifySelinWhenEarthquakeWhenEarthquakeOccured(
        ILogger<NotifySelinWhenEarthquakeWhenEarthquakeOccured> logger,
        DepremDbContext depremDbContext)
    {
        _logger = logger;
        _depremDbContext = depremDbContext;
    }
    
    public async Task Consume(ConsumeContext<EarthquakeOccurred> context)
    {
        EarthquakeOccurred earthquakeOccurredEvent = context.Message;
        
        _logger.LogInformation("NotifySelinWhenEarthquakeWhenEarthquakeOccured is started. EarthquakeSecondaryUniqueId: {EarthquakeSecondaryUniqueId}", earthquakeOccurredEvent.EarthquakeSecondaryUniqueId);

        Earthquake? earthquake = await _depremDbContext.Earthquakes.AsNoTracking().FirstOrDefaultAsync(e => e.SecondaryUniqueId == earthquakeOccurredEvent.EarthquakeSecondaryUniqueId);

        if (earthquake is null)
        {
            _logger.LogWarning("Earthquake is null");
            
            return;
        }

        Point location = new Point(0, 0);

        Task notifySelinIfEarthquakeOccurredNearHerTask = NotifySelinIfEarthquakeOccurredNearHer(earthquake, location: location);

        Task notifySelinIfEarthquakeMagnitudeIsAboveThresholdTask = NotifySelinIfEarthquakeMagnitudeIsAboveThreshold(earthquake, threshold: 3.9);

        Task notifySelinIfEarthquakeIsShallowTask = NotifySelinIfEarthquakeIsShallow(earthquake, maxDepthInKm: 10);

        Task notifySelinIfNearbyEarthquakeActivityIn24HoursIsAboveThresholdTask = NotifySelinIfNearbyEarthquakeActivityIn24HoursIsAboveThreshold(earthquake, radiusInMeters: 50, threshold: 5);

        Task notifySelinIfMagnitudeTrendIncreasedInTheLast48Hours = NotifySelinIfMagnitudeTrendIncreasedInTheLast48Hours(earthquake);

        await Task.WhenAll(
            notifySelinIfEarthquakeOccurredNearHerTask,
            notifySelinIfEarthquakeMagnitudeIsAboveThresholdTask,
            notifySelinIfEarthquakeIsShallowTask,
            notifySelinIfNearbyEarthquakeActivityIn24HoursIsAboveThresholdTask,
            notifySelinIfMagnitudeTrendIncreasedInTheLast48Hours);
        
        _logger.LogInformation("NotifySelinWhenEarthquakeWhenEarthquakeOccured is finished. EarthquakeSecondaryUniqueId: {EarthquakeSecondaryUniqueId}", earthquakeOccurredEvent.EarthquakeSecondaryUniqueId);
    }
    
    private Task NotifySelinIfEarthquakeOccurredNearHer(Earthquake earthquake, Point location)
    {
        throw new NotImplementedException();
    }
    
    private async Task NotifySelinIfEarthquakeMagnitudeIsAboveThreshold(Earthquake earthquake, double threshold)
    {
        throw new NotImplementedException();
    }
    
    private async Task NotifySelinIfEarthquakeIsShallow(Earthquake earthquake, double maxDepthInKm)
    {
        throw new NotImplementedException();
    }
    
    private async Task NotifySelinIfNearbyEarthquakeActivityIn24HoursIsAboveThreshold(Earthquake earthquake, double radiusInMeters, int threshold)
    {
        throw new NotImplementedException();
    }

    private Task NotifySelinIfMagnitudeTrendIncreasedInTheLast48Hours(Earthquake earthquake)
    {
        throw new NotImplementedException();
    }
}
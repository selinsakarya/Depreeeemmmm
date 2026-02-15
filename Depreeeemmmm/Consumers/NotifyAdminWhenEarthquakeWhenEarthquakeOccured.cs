using Depreeeemmmm.Data;
using Depreeeemmmm.Data.Entities;
using Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Depreeeemmmm.Consumers;

public class NotifyAdminWhenEarthquakeWhenEarthquakeOccured : IConsumer<EarthquakeOccurred>
{
    private readonly ILogger<NotifyAdminWhenEarthquakeWhenEarthquakeOccured> _logger;
    private readonly DepremDbContext _depremDbContext;

    public NotifyAdminWhenEarthquakeWhenEarthquakeOccured(
        ILogger<NotifyAdminWhenEarthquakeWhenEarthquakeOccured> logger,
        DepremDbContext depremDbContext)
    {
        _logger = logger;
        _depremDbContext = depremDbContext;
    }
    
    public async Task Consume(ConsumeContext<EarthquakeOccurred> context)
    {
        EarthquakeOccurred earthquakeOccurredEvent = context.Message;
        
        _logger.LogInformation("NotifyAdminWhenEarthquakeWhenEarthquakeOccured is started. EarthquakeSecondaryUniqueId: {EarthquakeSecondaryUniqueId}", earthquakeOccurredEvent.EarthquakeSecondaryUniqueId);

        Earthquake? earthquake = await _depremDbContext.Earthquakes.AsNoTracking().FirstOrDefaultAsync(e => e.SecondaryUniqueId == earthquakeOccurredEvent.EarthquakeSecondaryUniqueId, context.CancellationToken);

        if (earthquake is null)
        {
            _logger.LogWarning("Earthquake is null");
            
            return;
        }

        AdminLocation? adminLocation = await _depremDbContext.AdminLocations.AsNoTracking().OrderByDescending(a => a.CreatedAt).FirstOrDefaultAsync(context.CancellationToken);

        if (adminLocation is null)
        {
            _logger.LogWarning("AdminLocation is null");

            return;
        }
        
        bool isEarthquakeOccurredNearAdmin = IsEarthquakeOccurredNearAdmin(earthquake, adminLocation);

        if (isEarthquakeOccurredNearAdmin)
        {
            Task notifyAdminIfEarthquakeMagnitudeIsAboveThresholdTask = NotifyAdminIfEarthquakeMagnitudeIsAboveThreshold(earthquake, threshold: 3.9);

            Task notifyAdminIfEarthquakeIsShallowTask = NotifyAdminIfEarthquakeIsShallow(earthquake, maxDepthInKm: 10);
            
            await Task.WhenAll(
                notifyAdminIfEarthquakeMagnitudeIsAboveThresholdTask,
                notifyAdminIfEarthquakeIsShallowTask);
        }
        
        _logger.LogInformation("NotifyAdminWhenEarthquakeWhenEarthquakeOccured is finished. EarthquakeSecondaryUniqueId: {EarthquakeSecondaryUniqueId}", earthquakeOccurredEvent.EarthquakeSecondaryUniqueId);
    }

    private static bool IsEarthquakeOccurredNearAdmin(Earthquake earthquake, AdminLocation adminLocation)
    {
        double earthquakeLatitude = earthquake.Coordinates.Y;
        
        double earthquakeLongitude = earthquake.Coordinates.X;
        
        double distanceInKm = CalculateDistanceInKm(earthquakeLatitude, earthquakeLongitude, adminLocation.Latitude, adminLocation.longitude);

        const int maxDistanceInKm = 50;
        
        bool isEarthquakeOccurredNearAdmin = distanceInKm <= maxDistanceInKm;

        return isEarthquakeOccurredNearAdmin;
    }
    
    private static double CalculateDistanceInKm(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        double deltaLatitude = ToRadians(latitude2 - latitude1);
        double deltaLongitude = ToRadians(longitude2 - longitude1);

        double haversine = Math.Sin(deltaLatitude / 2) * Math.Sin(deltaLatitude / 2) +
                           Math.Cos(ToRadians(latitude1)) * Math.Cos(ToRadians(latitude2)) *
                           Math.Sin(deltaLongitude / 2) * Math.Sin(deltaLongitude / 2);

        double centralAngle = 2 * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1 - haversine));
    
        const double earthRadiusKm = 6371; 
        
        return earthRadiusKm * centralAngle;
    }

    private static double ToRadians(double angle) => angle * Math.PI / 180.0;
    
    private async Task NotifyAdminIfEarthquakeMagnitudeIsAboveThreshold(Earthquake earthquake, double threshold)
    {
        throw new NotImplementedException();
    }
    
    private async Task NotifyAdminIfEarthquakeIsShallow(Earthquake earthquake, double maxDepthInKm)
    {
        throw new NotImplementedException();
    }
    
}
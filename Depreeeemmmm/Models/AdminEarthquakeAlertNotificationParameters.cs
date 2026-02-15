using Depreeeemmmm.Data.Entities;

namespace Depreeeemmmm.Models;

public class AdminEarthquakeAlertNotificationParameters
{
    public Earthquake Earthquake { get; set; }
    
    public AdminLocation AdminLocation { get; set; }

    public double DistanceToAdminInKm { get; set; }

    public bool IsEarthquakeOccurredNearAdmin { get; set; }

    public bool IsMagnitudeAboveThreshold { get; set; }

    public bool IsDepthBelowThreshold { get; set; }

    public bool IsMagnitudeJumpDetected { get; set; }

    public bool IsDepthTrendGoingUpward { get; set; }

    public bool IsClusterDensityHigh { get; set; }

    public bool IsMagnitudeTrendGoingUpward { get; set; }
}
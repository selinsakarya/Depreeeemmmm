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

    public double MagnitudeJumpAmount { get; set; }

    public bool IsDepthTrendGoingUpward { get; set; }

    public double DepthTrendChangeKm { get; set; }

    public bool IsClusterDensityHigh { get; set; }

    public int NearbyEarthquakeCount { get; set; }

    public double? MaxNearbyMagnitude { get; set; }

    public double? AverageNearbyDepth { get; set; }

    public bool IsMagnitudeTrendGoingUpward { get; set; }

    public double EstimatedFeltIntensity { get; set; }

    public bool IsAftershock { get; set; }

    public double? MainshockMagnitude { get; set; }

    public double? HoursSinceMainshock { get; set; }

    public bool IsForeshockPattern { get; set; }

    public bool IsSwarmDetected { get; set; }

    public int SwarmEarthquakeCount { get; set; }

    public bool IsActivityAccelerating { get; set; }

    public double? ActivityAccelerationRatio { get; set; }

    public bool IsRepeatingEpicenter { get; set; }

    public int RepeatingEpicenterCount { get; set; }

    public bool IsDominantEnergyRelease { get; set; }

    public double? HoursSinceLastSignificantEarthquake { get; set; }

    public string RiskLevel { get; set; }
}

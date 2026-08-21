namespace Depreeeemmmm.Models;

public class HourlyLocationActivityReport
{
    public string Location { get; set; }
   
    public int TotalCount { get; set; }
    
    public double MaxMagnitude { get; set; }

    public Dictionary<DateTime, MinuteStatistic> MinuteStatistics { get; set; } = new();
    
    public Dictionary<double, int> MagnitudeDistribution { get; set; } = new();

    public LocationActivityInsights Insights { get; set; } = new();
}

public class MinuteStatistic
{
    public int Count { get; set; }
    
    public double MaxMagnitude { get; set; }
}

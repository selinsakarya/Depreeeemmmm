namespace Depreeeemmmm.Models;

public class DailyLocationActivityReport
{
    public string Location { get; set; }
   
    public int TotalCount { get; set; }
    
    public double MaxMagnitude { get; set; }

    public Dictionary<int, HourlyStatistic> HourlyStatistics { get; set; } = new Dictionary<int, HourlyStatistic>();
    
    public Dictionary<double, int> MagnitudeDistribution { get; set; } = new  Dictionary<double, int>();

    public LocationActivityInsights Insights { get; set; } = new();
}

public class HourlyStatistic
{
    public int Count { get; set; }
    
    public double MaxMagnitude { get; set; }
}

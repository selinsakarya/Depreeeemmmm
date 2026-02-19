namespace Depreeeemmmm.Models;

public class WeeklyLocationActivityReport
{
    public string Location { get; set; }
   
    public int TotalCount { get; set; }
    
    public double MaxMagnitude { get; set; }

    public Dictionary<DateTime, DailyStatistic> DailyStatistics { get; set; } = new();
    
    public Dictionary<double, int> MagnitudeDistribution { get; set; } = new();
}

public class DailyStatistic
{
    public int Count { get; set; }
    
    public double MaxMagnitude { get; set; }
}

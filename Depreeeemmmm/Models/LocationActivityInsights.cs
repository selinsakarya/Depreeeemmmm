namespace Depreeeemmmm.Models;

public class LocationActivityInsights
{
    public double AverageMagnitude { get; set; }

    public double AverageDepth { get; set; }

    public double MinDepth { get; set; }

    public int ShallowEarthquakeCount { get; set; }

    public int SignificantEarthquakeCount { get; set; }

    public string ActivityPattern { get; set; }

    public string MagnitudeTrend { get; set; }

    public string DepthTrend { get; set; }

    public string RiskLevel { get; set; }

    public int ActivityScore { get; set; }

    public int? PreviousPeriodCount { get; set; }

    public double? ActivityChangeRatio { get; set; }

    public string? PeakActivityLabel { get; set; }

    public List<string> Highlights { get; set; } = new();
}

public class PeriodEarthquakeSummary
{
    public int TotalCount { get; set; }

    public double MaxMagnitude { get; set; }

    public string? StrongestLocation { get; set; }

    public int ActiveRegionCount { get; set; }

    public int SignificantCount { get; set; }

    public int ShallowCount { get; set; }

    public int PreviousPeriodTotalCount { get; set; }

    public double? ActivityChangeRatio { get; set; }

    public string OverallRiskLevel { get; set; }

    public List<string> NotableFindings { get; set; } = new();
}

using Depreeeemmmm.Data.Entities;
using Depreeeemmmm.Models;

namespace Depreeeemmmm.Services;

public static class EarthquakeReportAnalyzer
{
    private const double ShallowDepthThresholdKm = 10;
    private const double SignificantMagnitudeThreshold = 4.0;
    private const double SwarmAverageMagnitudeThreshold = 3.2;

    public static LocationActivityInsights AnalyzeLocation(List<Earthquake> earthquakes, int? previousPeriodCount = null)
    {
        if (earthquakes.Count == 0)
        {
            return new LocationActivityInsights
            {
                ActivityPattern = "Veri yok",
                MagnitudeTrend = "-",
                DepthTrend = "-",
                RiskLevel = "Düşük"
            };
        }

        List<Earthquake> chronological = earthquakes.OrderBy(e => e.OccurredAt).ToList();

        double averageMagnitude = Math.Round(chronological.Average(e => e.Magnitude), 2);
        double averageDepth = Math.Round(chronological.Average(e => e.Depth), 1);
        double minDepth = Math.Round(chronological.Min(e => e.Depth), 1);
        double maxMagnitude = chronological.Max(e => e.Magnitude);
        int shallowCount = chronological.Count(e => e.Depth < ShallowDepthThresholdKm);
        int significantCount = chronological.Count(e => e.Magnitude >= SignificantMagnitudeThreshold);

        string activityPattern = DetectActivityPattern(chronological);
        string magnitudeTrend = DetectMagnitudeTrend(chronological);
        string depthTrend = DetectDepthTrend(chronological);
        string riskLevel = CalculateLocationRiskLevel(chronological, maxMagnitude, shallowCount, significantCount, activityPattern, magnitudeTrend);

        double? activityChangeRatio = null;

        if (previousPeriodCount.HasValue)
        {
            activityChangeRatio = previousPeriodCount.Value == 0
                ? chronological.Count
                : Math.Round((double)chronological.Count / previousPeriodCount.Value, 1);
        }

        LocationActivityInsights insights = new LocationActivityInsights
        {
            AverageMagnitude = averageMagnitude,
            AverageDepth = averageDepth,
            MinDepth = minDepth,
            ShallowEarthquakeCount = shallowCount,
            SignificantEarthquakeCount = significantCount,
            ActivityPattern = activityPattern,
            MagnitudeTrend = magnitudeTrend,
            DepthTrend = depthTrend,
            RiskLevel = riskLevel,
            PreviousPeriodCount = previousPeriodCount,
            ActivityChangeRatio = activityChangeRatio,
            ActivityScore = CalculateActivityScore(chronological.Count, maxMagnitude, significantCount, riskLevel),
            Highlights = BuildLocationHighlights(chronological, activityPattern, magnitudeTrend, depthTrend, shallowCount, significantCount, activityChangeRatio)
        };

        return insights;
    }

    public static PeriodEarthquakeSummary AnalyzePeriod(List<Earthquake> currentPeriodEarthquakes, List<Earthquake>? previousPeriodEarthquakes = null)
    {
        if (currentPeriodEarthquakes.Count == 0)
        {
            return new PeriodEarthquakeSummary
            {
                OverallRiskLevel = "Düşük"
            };
        }

        Earthquake strongest = currentPeriodEarthquakes.OrderByDescending(e => e.Magnitude).First();
        int significantCount = currentPeriodEarthquakes.Count(e => e.Magnitude >= SignificantMagnitudeThreshold);
        int shallowCount = currentPeriodEarthquakes.Count(e => e.Depth < ShallowDepthThresholdKm);
        int activeRegionCount = currentPeriodEarthquakes.Where(e => e.Location != null).Select(e => e.Location!).Distinct().Count();
        int previousTotal = previousPeriodEarthquakes?.Count ?? 0;

        double? changeRatio = previousTotal == 0
            ? (currentPeriodEarthquakes.Count > 0 ? currentPeriodEarthquakes.Count : null)
            : Math.Round((double)currentPeriodEarthquakes.Count / previousTotal, 1);

        string overallRisk = CalculatePeriodRiskLevel(currentPeriodEarthquakes, significantCount, shallowCount, changeRatio);

        PeriodEarthquakeSummary summary = new PeriodEarthquakeSummary
        {
            TotalCount = currentPeriodEarthquakes.Count,
            MaxMagnitude = strongest.Magnitude,
            StrongestLocation = strongest.Location,
            ActiveRegionCount = activeRegionCount,
            SignificantCount = significantCount,
            ShallowCount = shallowCount,
            PreviousPeriodTotalCount = previousTotal,
            ActivityChangeRatio = changeRatio,
            OverallRiskLevel = overallRisk,
            NotableFindings = BuildPeriodFindings(currentPeriodEarthquakes, significantCount, shallowCount, changeRatio, strongest)
        };

        return summary;
    }

    private static string DetectActivityPattern(List<Earthquake> chronological)
    {
        if (chronological.Count >= 8 && chronological.Average(e => e.Magnitude) < SwarmAverageMagnitudeThreshold)
        {
            return "Swarm";
        }

        TimeSpan duration = chronological[^1].OccurredAt - chronological[0].OccurredAt;
        double quakesPerHour = duration.TotalHours < 0.1
            ? chronological.Count
            : chronological.Count / duration.TotalHours;

        if (quakesPerHour >= 5)
        {
            return "Patlama";
        }

        if (chronological.Count >= 4 && IsIncreasingSequence(chronological.Select(e => e.Magnitude).ToList()))
        {
            return "Artan dizi";
        }

        if (chronological.Count >= 5)
        {
            int mid = chronological.Count / 2;
            double firstHalfRate = mid / Math.Max((chronological[mid].OccurredAt - chronological[0].OccurredAt).TotalHours, 0.1);
            double secondHalfRate = (chronological.Count - mid) / Math.Max((chronological[^1].OccurredAt - chronological[mid].OccurredAt).TotalHours, 0.1);

            if (secondHalfRate >= firstHalfRate * 2)
            {
                return "Hızlanan";
            }
        }

        return "Normal";
    }

    private static string DetectMagnitudeTrend(List<Earthquake> chronological)
    {
        if (chronological.Count < 3)
        {
            return "Stabil";
        }

        int mid = chronological.Count / 2;
        double firstHalfAvg = chronological.Take(mid).Average(e => e.Magnitude);
        double secondHalfAvg = chronological.Skip(mid).Average(e => e.Magnitude);
        double difference = secondHalfAvg - firstHalfAvg;

        if (difference >= 0.3)
        {
            return "Yükseliyor";
        }

        if (difference <= -0.3)
        {
            return "Düşüyor";
        }

        return "Stabil";
    }

    private static string DetectDepthTrend(List<Earthquake> chronological)
    {
        if (chronological.Count < 3)
        {
            return "Stabil";
        }

        int mid = chronological.Count / 2;
        double firstHalfAvg = chronological.Take(mid).Average(e => e.Depth);
        double secondHalfAvg = chronological.Skip(mid).Average(e => e.Depth);
        double difference = firstHalfAvg - secondHalfAvg;

        if (difference >= 2)
        {
            return "Sığlaşıyor";
        }

        if (difference <= -2)
        {
            return "Derinleşiyor";
        }

        return "Stabil";
    }

    private static string CalculateLocationRiskLevel(
        List<Earthquake> chronological,
        double maxMagnitude,
        int shallowCount,
        int significantCount,
        string activityPattern,
        string magnitudeTrend)
    {
        int score = 0;

        if (maxMagnitude >= 5.0) score += 4;
        else if (maxMagnitude >= 4.5) score += 3;
        else if (maxMagnitude >= 4.0) score += 2;
        else if (maxMagnitude >= 3.5) score += 1;

        if (shallowCount >= chronological.Count / 2 && maxMagnitude >= 3.5) score += 2;
        if (significantCount >= 2) score += 2;
        if (activityPattern is "Patlama" or "Artan dizi") score += 2;
        if (activityPattern == "Swarm" && chronological.Count >= 12) score += 1;
        if (magnitudeTrend == "Yükseliyor") score += 1;

        return score switch
        {
            >= 6 => "Kritik",
            >= 4 => "Yüksek",
            >= 2 => "Orta",
            _ => "Düşük"
        };
    }

    private static string CalculatePeriodRiskLevel(List<Earthquake> earthquakes, int significantCount, int shallowCount, double? changeRatio)
    {
        double maxMagnitude = earthquakes.Max(e => e.Magnitude);
        int score = 0;

        if (maxMagnitude >= 5.0) score += 3;
        else if (maxMagnitude >= 4.0) score += 2;

        if (significantCount >= 3) score += 2;
        if (shallowCount >= earthquakes.Count / 2 && maxMagnitude >= 4.0) score += 2;
        if (changeRatio >= 2.0) score += 2;
        if (earthquakes.Count >= 30) score += 1;

        return score switch
        {
            >= 5 => "Kritik",
            >= 3 => "Yüksek",
            >= 1 => "Orta",
            _ => "Düşük"
        };
    }

    private static int CalculateActivityScore(int totalCount, double maxMagnitude, int significantCount, string riskLevel)
    {
        int riskBonus = riskLevel switch
        {
            "Kritik" => 100,
            "Yüksek" => 50,
            "Orta" => 20,
            _ => 0
        };

        return totalCount + significantCount * 10 + (int)(maxMagnitude * 5) + riskBonus;
    }

    private static List<string> BuildLocationHighlights(
        List<Earthquake> chronological,
        string activityPattern,
        string magnitudeTrend,
        string depthTrend,
        int shallowCount,
        int significantCount,
        double? activityChangeRatio)
    {
        List<string> highlights = new List<string>();

        if (activityPattern != "Normal")
        {
            highlights.Add($"Aktivite deseni: {activityPattern}");
        }

        if (magnitudeTrend != "Stabil")
        {
            highlights.Add($"Büyüklük trendi {magnitudeTrend.ToLower()}");
        }

        if (depthTrend != "Stabil")
        {
            highlights.Add($"Derinlik {depthTrend.ToLower()}");
        }

        if (significantCount > 0)
        {
            highlights.Add($"{significantCount} adet M{SignificantMagnitudeThreshold}+ deprem");
        }

        if (shallowCount >= chronological.Count / 2 && chronological.Count >= 3)
        {
            highlights.Add($"Depremlerin %{(int)Math.Round((double)shallowCount / chronological.Count * 100)}'i sığ (<{ShallowDepthThresholdKm} km)");
        }

        if (activityChangeRatio.HasValue)
        {
            if (activityChangeRatio >= 2.0)
            {
                highlights.Add($"Önceki döneme göre {activityChangeRatio:F1}x daha aktif");
            }
            else if (activityChangeRatio <= 0.5 && activityChangeRatio > 0)
            {
                highlights.Add($"Önceki döneme göre aktivite azaldı ({activityChangeRatio:F1}x)");
            }
        }

        Earthquake? strongest = chronological.OrderByDescending(e => e.Magnitude).FirstOrDefault();

        if (strongest is not null && strongest.Depth < ShallowDepthThresholdKm && strongest.Magnitude >= 3.5)
        {
            highlights.Add($"En güçlü deprem sığ: M{strongest.Magnitude:F1} / {strongest.Depth:F1} km");
        }

        return highlights;
    }

    private static List<string> BuildPeriodFindings(
        List<Earthquake> earthquakes,
        int significantCount,
        int shallowCount,
        double? changeRatio,
        Earthquake strongest)
    {
        List<string> findings = new List<string>();

        if (changeRatio >= 2.0)
        {
            findings.Add($"Ülke geneli aktivite önceki döneme göre {changeRatio:F1}x arttı");
        }
        else if (changeRatio is <= 0.5 and > 0)
        {
            findings.Add("Ülke geneli aktivite önceki döneme göre azaldı");
        }

        if (significantCount > 0)
        {
            findings.Add($"{significantCount} adet M{SignificantMagnitudeThreshold}+ deprem kaydedildi");
        }

        if (strongest.Magnitude >= 4.0)
        {
            findings.Add($"En güçlü: M{strongest.Magnitude:F1} — {strongest.Location} ({strongest.Depth:F1} km derinlik)");
        }

        if (shallowCount >= earthquakes.Count / 2 && earthquakes.Count >= 5)
        {
            findings.Add("Depremlerin çoğu sığ derinlikte — yerel etki potansiyeli yüksek");
        }

        Dictionary<string, int> regionCounts = earthquakes
            .Where(e => e.Location != null)
            .GroupBy(e => e.Location!)
            .ToDictionary(g => g.Key, g => g.Count());

        if (regionCounts.Count >= 2)
        {
            KeyValuePair<string, int> hottest = regionCounts.OrderByDescending(x => x.Value).First();

            if (hottest.Value >= 5)
            {
                findings.Add($"En aktif bölge: {hottest.Key} ({hottest.Value} deprem)");
            }
        }

        return findings;
    }

    private static bool IsIncreasingSequence(List<double> values)
    {
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] <= values[i - 1])
            {
                return false;
            }
        }

        return true;
    }
}

using Depreeeemmmm.Data.Entities;
using Depreeeemmmm.Models;
using Depreeeemmmm.Services;

namespace Depreeeemmmm.Extensions;

public static class ListExtensions
{
    public static Dictionary<string, DailyLocationActivityReport> ToDailyLocationActivityReport(
        this List<Earthquake> earthquakes,
        IReadOnlyDictionary<string, int>? previousCountByLocation = null)
    {
        Dictionary<string, DailyLocationActivityReport> locationActivityReport = new Dictionary<string, DailyLocationActivityReport>();
        Dictionary<string, List<Earthquake>> earthquakesByLocation = GroupEarthquakesByLocation(earthquakes);

        foreach ((string location, List<Earthquake> locationEarthquakes) in earthquakesByLocation)
        {
            DailyLocationActivityReport locationReport = new DailyLocationActivityReport
            {
                Location = location,
                MaxMagnitude = locationEarthquakes.Max(e => e.Magnitude),
                TotalCount = locationEarthquakes.Count
            };

            foreach (Earthquake earthquake in locationEarthquakes)
            {
                int hour = earthquake.OccurredAt.Hour;

                if (locationReport.HourlyStatistics.TryGetValue(hour, out HourlyStatistic? hourlyStatistic) is false)
                {
                    hourlyStatistic = new HourlyStatistic
                    {
                        Count = 0,
                        MaxMagnitude = earthquake.Magnitude
                    };

                    locationReport.HourlyStatistics[hour] = hourlyStatistic;
                }

                hourlyStatistic.Count++;

                if (earthquake.Magnitude > hourlyStatistic.MaxMagnitude)
                {
                    hourlyStatistic.MaxMagnitude = earthquake.Magnitude;
                }

                double magnitudeKey = Math.Round(earthquake.Magnitude, 1);

                if (locationReport.MagnitudeDistribution.TryAdd(magnitudeKey, 1) is false)
                {
                    locationReport.MagnitudeDistribution[magnitudeKey] += 1;
                }
            }

            int? previousCount = null;

            if (previousCountByLocation is not null && previousCountByLocation.TryGetValue(location, out int count))
            {
                previousCount = count;
            }

            locationReport.Insights = EarthquakeReportAnalyzer.AnalyzeLocation(locationEarthquakes, previousCount);
            locationActivityReport[location] = locationReport;
        }

        return locationActivityReport;
    }

    public static Dictionary<string, WeeklyLocationActivityReport> ToWeeklyLocationActivityReport(
        this List<Earthquake> earthquakes,
        IReadOnlyDictionary<string, int>? previousCountByLocation = null)
    {
        Dictionary<string, WeeklyLocationActivityReport> locationActivityReport = new();
        Dictionary<string, List<Earthquake>> earthquakesByLocation = GroupEarthquakesByLocation(earthquakes);

        foreach ((string location, List<Earthquake> locationEarthquakes) in earthquakesByLocation)
        {
            WeeklyLocationActivityReport locationReport = new WeeklyLocationActivityReport
            {
                Location = location,
                MaxMagnitude = locationEarthquakes.Max(e => e.Magnitude),
                TotalCount = locationEarthquakes.Count
            };

            foreach (Earthquake earthquake in locationEarthquakes)
            {
                DateTime day = earthquake.OccurredAt.Date;

                if (locationReport.DailyStatistics.TryGetValue(day, out DailyStatistic? dailyStatistic) is false)
                {
                    dailyStatistic = new DailyStatistic
                    {
                        Count = 0,
                        MaxMagnitude = earthquake.Magnitude
                    };

                    locationReport.DailyStatistics[day] = dailyStatistic;
                }

                dailyStatistic.Count++;

                if (earthquake.Magnitude > dailyStatistic.MaxMagnitude)
                {
                    dailyStatistic.MaxMagnitude = earthquake.Magnitude;
                }

                double magnitudeKey = Math.Round(earthquake.Magnitude, 1);

                if (locationReport.MagnitudeDistribution.TryAdd(magnitudeKey, 1) is false)
                {
                    locationReport.MagnitudeDistribution[magnitudeKey] += 1;
                }
            }

            int? previousCount = null;

            if (previousCountByLocation is not null && previousCountByLocation.TryGetValue(location, out int count))
            {
                previousCount = count;
            }

            locationReport.Insights = EarthquakeReportAnalyzer.AnalyzeLocation(locationEarthquakes, previousCount);
            locationActivityReport[location] = locationReport;
        }

        return locationActivityReport;
    }

    public static Dictionary<string, HourlyLocationActivityReport> ToHourlyLocationActivityReport(
        this List<Earthquake> earthquakes,
        IReadOnlyDictionary<string, int>? previousCountByLocation = null)
    {
        Dictionary<string, HourlyLocationActivityReport> locationActivityReport = new();
        Dictionary<string, List<Earthquake>> earthquakesByLocation = GroupEarthquakesByLocation(earthquakes);

        foreach ((string location, List<Earthquake> locationEarthquakes) in earthquakesByLocation)
        {
            HourlyLocationActivityReport locationReport = new HourlyLocationActivityReport
            {
                Location = location,
                MaxMagnitude = locationEarthquakes.Max(e => e.Magnitude),
                TotalCount = locationEarthquakes.Count
            };

            foreach (Earthquake earthquake in locationEarthquakes)
            {
                DateTime minuteKey = earthquake.OccurredAt
                    .AddSeconds(-earthquake.OccurredAt.Second)
                    .AddMilliseconds(-earthquake.OccurredAt.Millisecond);

                if (locationReport.MinuteStatistics.TryGetValue(minuteKey, out MinuteStatistic? minuteStatistic) is false)
                {
                    minuteStatistic = new MinuteStatistic
                    {
                        Count = 0,
                        MaxMagnitude = earthquake.Magnitude
                    };

                    locationReport.MinuteStatistics[minuteKey] = minuteStatistic;
                }

                minuteStatistic.Count++;

                if (earthquake.Magnitude > minuteStatistic.MaxMagnitude)
                {
                    minuteStatistic.MaxMagnitude = earthquake.Magnitude;
                }

                double magnitudeKey = Math.Round(earthquake.Magnitude, 1);

                if (locationReport.MagnitudeDistribution.TryAdd(magnitudeKey, 1) is false)
                {
                    locationReport.MagnitudeDistribution[magnitudeKey] += 1;
                }
            }

            int? previousCount = null;

            if (previousCountByLocation is not null && previousCountByLocation.TryGetValue(location, out int count))
            {
                previousCount = count;
            }

            locationReport.Insights = EarthquakeReportAnalyzer.AnalyzeLocation(locationEarthquakes, previousCount);
            locationActivityReport[location] = locationReport;
        }

        return locationActivityReport;
    }

    public static Dictionary<string, int> ToLocationCountByLocation(this List<Earthquake> earthquakes)
    {
        return earthquakes
            .Where(e => e.Location != null)
            .GroupBy(e => e.Location!)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static Dictionary<string, List<Earthquake>> GroupEarthquakesByLocation(List<Earthquake> earthquakes)
    {
        return earthquakes
            .Where(e => e.Location != null)
            .GroupBy(e => e.Location!)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}

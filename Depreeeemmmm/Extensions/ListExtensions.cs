using System.Text;
using Depreeeemmmm.Data.Entities;
using Depreeeemmmm.Models;

namespace Depreeeemmmm.Extensions;

public static class ListExtensions
{
    public static Dictionary<string, DailyLocationActivityReport> ToDailyLocationActivityReport(this List<Earthquake> earthquakes, DateTime startDate, DateTime endDate)
    {
        Dictionary<string, DailyLocationActivityReport> locationActivityReport = new Dictionary<string, DailyLocationActivityReport>();

        foreach (Earthquake earthquake in earthquakes)
        {
            if (locationActivityReport.TryGetValue(earthquake.Location, out DailyLocationActivityReport? locationReport))
            {
                locationReport.TotalCount += 1;
            }
            else
            {
                locationReport = new DailyLocationActivityReport
                {
                    Location = earthquake.Location,
                    MaxMagnitude = earthquake.Magnitude,
                    TotalCount = 1,
                    StartDate =  startDate,
                    EndDate = endDate
                };

                locationActivityReport[earthquake.Location] = locationReport;
            }

            if (earthquake.Magnitude > locationReport.MaxMagnitude)
            {
                locationReport.MaxMagnitude = earthquake.Magnitude;
            }

            int hour = earthquake.OccurredAt.Hour;

            if (locationReport.HourlyStatistics.TryGetValue(hour, out var hourlyStatistic))
            {
                hourlyStatistic.Count++;
            }
            else
            {
                hourlyStatistic = new HourlyStatistic()
                {
                    Count = 1,
                    MaxMagnitude = earthquake.Magnitude
                };

                locationReport.HourlyStatistics[hour] = hourlyStatistic;
            }

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

        return locationActivityReport;
    }
}
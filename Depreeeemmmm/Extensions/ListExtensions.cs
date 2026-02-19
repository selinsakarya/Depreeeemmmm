using System.Text;
using Depreeeemmmm.Data.Entities;
using Depreeeemmmm.Models;

namespace Depreeeemmmm.Extensions;

public static class ListExtensions
{
    public static Dictionary<string, DailyLocationActivityReport> ToLocationActivityReport(this List<Earthquake> earthquakes, LocationActivityReportType type, DateTime startDate, DateTime endDate)
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
                    Type = type,
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
    
    public static string ToTelegramMessage(this List<DailyLocationActivityReport> locationActivityReports)
    {
        StringBuilder sb = new StringBuilder();

        TimeZoneInfo turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        
        foreach (DailyLocationActivityReport locationActivityReport in locationActivityReports)
        {
            sb.AppendLine($"{locationActivityReport.Type} Deprem Özeti");
            
            DateTime turkeyStartDate = TimeZoneInfo.ConvertTimeFromUtc(locationActivityReport.StartDate, turkeyTimeZone);
        
            DateTime turkeyEndDate = TimeZoneInfo.ConvertTimeFromUtc(locationActivityReport.EndDate, turkeyTimeZone);
        
            sb.AppendLine($"{turkeyEndDate} - {turkeyStartDate}");
            
            sb.AppendLine($"📍 {locationActivityReport.Location}");
            
            sb.AppendLine($"Toplam: {locationActivityReport.TotalCount} Deprem");
            
            sb.AppendLine($"Max: {Math.Round(locationActivityReport.MaxMagnitude, 2)}");
    
            if (locationActivityReport.HourlyStatistics.Any())
            {
                sb.AppendLine("Saatlik Dağılım:");

                foreach (KeyValuePair<int, HourlyStatistic> hour in locationActivityReport.HourlyStatistics.OrderBy(x => x.Key))
                {
                    sb.AppendLine($"  {hour.Key:00}:00 → {hour.Value.Count} (Max {Math.Round(hour.Value.MaxMagnitude, 2)})");
                }
            }

            if (locationActivityReport.MagnitudeDistribution.Any())
            {
                sb.AppendLine("Büyüklük Dağılımı:");
        
                foreach (var mag in locationActivityReport.MagnitudeDistribution.OrderByDescending(x => x.Key))
                {
                    sb.AppendLine($"  {mag.Key:F1} → {mag.Value}");
                }
            }

            sb.AppendLine(new string('-', 30));
        }

        string telegramMessage = sb.ToString();
        
        return telegramMessage;
    }
}
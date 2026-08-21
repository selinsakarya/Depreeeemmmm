using System.Text;
using Depreeeemmmm.Models;

namespace Depreeeemmmm.Services;

public static class EarthquakeReportMessageBuilder
{
    public static string BuildHourlyReport(
        PeriodEarthquakeSummary periodSummary,
        List<HourlyLocationActivityReport> locationReports,
        DateTime periodStartUtc,
        DateTime periodEndUtc)
    {
        StringBuilder sb = new StringBuilder();
        TimeZoneInfo turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

        DateTime turkeyStart = TimeZoneInfo.ConvertTimeFromUtc(periodStartUtc, turkeyTimeZone);
        DateTime turkeyEnd = TimeZoneInfo.ConvertTimeFromUtc(periodEndUtc, turkeyTimeZone);

        AppendPeriodHeader(sb, "Saatlik Deprem Raporu", turkeyStart, turkeyEnd, periodSummary);

        foreach (HourlyLocationActivityReport report in locationReports)
        {
            AppendLocationHeader(sb, report.Location, report.TotalCount, report.MaxMagnitude, report.Insights);
            AppendInsights(sb, report.Insights);

            if (report.MinuteStatistics.Any())
            {
                sb.AppendLine("Dakikalık Dağılım:");

                foreach (KeyValuePair<DateTime, MinuteStatistic> minute in report.MinuteStatistics.OrderBy(x => x.Key))
                {
                    DateTime turkeyMinute = TimeZoneInfo.ConvertTimeFromUtc(minute.Key, turkeyTimeZone);
                    sb.AppendLine($"  {turkeyMinute:HH:mm} → {minute.Value.Count} (Max M{minute.Value.MaxMagnitude:F1})");
                }
            }

            AppendMagnitudeDistribution(sb, report.MagnitudeDistribution);
            AppendSeparator(sb);
        }

        return sb.ToString();
    }

    public static string BuildDailyReport(
        PeriodEarthquakeSummary periodSummary,
        List<DailyLocationActivityReport> locationReports,
        DateTime periodStartUtc,
        DateTime periodEndUtc)
    {
        StringBuilder sb = new StringBuilder();
        TimeZoneInfo turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

        DateTime turkeyStart = TimeZoneInfo.ConvertTimeFromUtc(periodStartUtc, turkeyTimeZone);
        DateTime turkeyEnd = TimeZoneInfo.ConvertTimeFromUtc(periodEndUtc, turkeyTimeZone);

        AppendPeriodHeader(sb, "Günlük Deprem Raporu", turkeyStart, turkeyEnd, periodSummary);

        foreach (DailyLocationActivityReport report in locationReports)
        {
            AppendLocationHeader(sb, report.Location, report.TotalCount, report.MaxMagnitude, report.Insights);
            AppendInsights(sb, report.Insights);

            if (report.HourlyStatistics.Any())
            {
                sb.AppendLine("Saatlik Dağılım:");

                foreach (KeyValuePair<int, HourlyStatistic> hour in report.HourlyStatistics.OrderBy(x => x.Key))
                {
                    sb.AppendLine($"  {hour.Key:00}:00 → {hour.Value.Count} (Max M{hour.Value.MaxMagnitude:F1})");
                }
            }

            AppendMagnitudeDistribution(sb, report.MagnitudeDistribution);
            AppendSeparator(sb);
        }

        return sb.ToString();
    }

    public static string BuildWeeklyReport(
        PeriodEarthquakeSummary periodSummary,
        List<WeeklyLocationActivityReport> locationReports,
        DateTime periodStartUtc,
        DateTime periodEndUtc)
    {
        StringBuilder sb = new StringBuilder();
        TimeZoneInfo turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

        DateTime turkeyStart = TimeZoneInfo.ConvertTimeFromUtc(periodStartUtc, turkeyTimeZone);
        DateTime turkeyEnd = TimeZoneInfo.ConvertTimeFromUtc(periodEndUtc, turkeyTimeZone);

        AppendPeriodHeader(sb, "Haftalık Deprem Raporu", turkeyStart, turkeyEnd, periodSummary);

        foreach (WeeklyLocationActivityReport report in locationReports)
        {
            AppendLocationHeader(sb, report.Location, report.TotalCount, report.MaxMagnitude, report.Insights);
            AppendInsights(sb, report.Insights);

            if (report.DailyStatistics.Any())
            {
                sb.AppendLine("Günlük Dağılım:");

                foreach (KeyValuePair<DateTime, DailyStatistic> day in report.DailyStatistics.OrderBy(x => x.Key))
                {
                    DateTime turkeyDay = TimeZoneInfo.ConvertTimeFromUtc(day.Key, turkeyTimeZone);
                    sb.AppendLine($"  {turkeyDay:dd.MM.yyyy} → {day.Value.Count} (Max M{day.Value.MaxMagnitude:F1})");
                }
            }

            AppendMagnitudeDistribution(sb, report.MagnitudeDistribution);
            AppendSeparator(sb);
        }

        return sb.ToString();
    }

    private static void AppendPeriodHeader(StringBuilder sb, string title, DateTime turkeyStart, DateTime turkeyEnd, PeriodEarthquakeSummary summary)
    {
        sb.AppendLine(title);
        sb.AppendLine($"{turkeyStart:dd.MM.yyyy HH:mm} - {turkeyEnd:dd.MM.yyyy HH:mm}");
        sb.AppendLine($"Genel Risk: {GetRiskEmoji(summary.OverallRiskLevel)} {summary.OverallRiskLevel}");
        sb.AppendLine($"Toplam: {summary.TotalCount} deprem | {summary.ActiveRegionCount} aktif bölge | Max M{summary.MaxMagnitude:F1}");

        if (summary.PreviousPeriodTotalCount > 0 && summary.ActivityChangeRatio.HasValue)
        {
            sb.AppendLine($"Önceki dönem: {summary.PreviousPeriodTotalCount} deprem ({summary.ActivityChangeRatio:F1}x)");
        }

        if (summary.NotableFindings.Any())
        {
            sb.AppendLine();
            sb.AppendLine("📋 Öne Çıkanlar:");

            foreach (string finding in summary.NotableFindings)
            {
                sb.AppendLine($"• {finding}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("🏙 En Aktif Bölgeler:");
        sb.AppendLine();
    }

    private static void AppendLocationHeader(StringBuilder sb, string location, int totalCount, double maxMagnitude, LocationActivityInsights insights)
    {
        sb.AppendLine($"📍 {location} — {GetRiskEmoji(insights.RiskLevel)} {insights.RiskLevel}");
        sb.AppendLine($"Toplam: {totalCount} | Max: M{maxMagnitude:F1} | Ort: M{insights.AverageMagnitude:F1} / {insights.AverageDepth:F1} km derinlik");
    }

    private static void AppendInsights(StringBuilder sb, LocationActivityInsights insights)
    {
        if (insights.Highlights.Any())
        {
            sb.AppendLine("🔍 Analiz:");

            foreach (string highlight in insights.Highlights)
            {
                sb.AppendLine($"• {highlight}");
            }
        }

        if (insights.SignificantEarthquakeCount == 0 && insights.ActivityPattern == "Normal" && !insights.Highlights.Any())
        {
            sb.AppendLine("🔍 Analiz: Rutin aktivite");
        }

        sb.AppendLine();
    }

    private static void AppendMagnitudeDistribution(StringBuilder sb, Dictionary<double, int> magnitudeDistribution)
    {
        if (!magnitudeDistribution.Any())
        {
            return;
        }

        sb.Append("Büyüklük: ");

        foreach (KeyValuePair<double, int> mag in magnitudeDistribution.OrderByDescending(x => x.Key))
        {
            sb.Append($"{mag.Value}x M{mag.Key:F1} / ");
        }

        sb.AppendLine();
        sb.AppendLine();
    }

    private static void AppendSeparator(StringBuilder sb)
    {
        sb.AppendLine(new string('-', 30));
        sb.AppendLine();
    }

    private static string GetRiskEmoji(string riskLevel)
    {
        return riskLevel switch
        {
            "Kritik" => "🔴",
            "Yüksek" => "🟠",
            "Orta" => "🟡",
            _ => "🟢"
        };
    }
}

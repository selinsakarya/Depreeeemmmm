using System.Text;
using Depreeeemmmm.Constants;
using Depreeeemmmm.Data;
using Depreeeemmmm.Data.Entities;
using Depreeeemmmm.Extensions;
using Depreeeemmmm.Models;
using Depreeeemmmm.Proxies;
using Depreeeemmmm.Proxies.TelegramApiProxy;
using Depreeeemmmm.Proxies.TelegramApiProxy.Models.Requests;
using Depreeeemmmm.Proxies.TelegramApiProxy.Models.Responses;
using Depreeeemmmm.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Depreeeemmmm.Jobs;

[DisallowConcurrentExecution]
public class WeeklyEarthquakeAnalyser : IJob
{
    private readonly ILogger<WeeklyEarthquakeAnalyser> _logger;
    private readonly DepremDbContext _depremDbContext;
    private readonly ITelegramApiProxy _telegramApiProxy;
    private readonly IConfigurationService _configurationService;

    public WeeklyEarthquakeAnalyser(
        ILogger<WeeklyEarthquakeAnalyser> logger,
        DepremDbContext depremDbContext,
        ITelegramApiProxy telegramApiProxy,
        IConfigurationService configurationService)
    {
        _logger = logger;
        _depremDbContext = depremDbContext;
        _telegramApiProxy = telegramApiProxy;
        _configurationService = configurationService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("WeeklyEarthquakeAnalyser is started");

        DateTime now = DateTime.UtcNow;

        DateTime sevenDaysAgo = now.AddDays(-7);

        List<Earthquake> earthquakes = await _depremDbContext.Earthquakes.AsNoTracking().Where(e => e.OccurredAt >= sevenDaysAgo && e.OccurredAt < now && e.Location != null).ToListAsync();

        if (earthquakes.Count == 0)
        {
            _logger.LogInformation($"No earthquakes found. Start: {sevenDaysAgo} End: {now}");

            return;
        }

        Dictionary<string, WeeklyLocationActivityReport> result = earthquakes.ToWeeklyLocationActivityReport();

        List<WeeklyLocationActivityReport> weeklyLocationActivityReports = result.Values
            .OrderByDescending(x => x.TotalCount)
            .Take(3)
            .ToList();

        string telegramMessage = CreateWeeklyTelegramMessage(weeklyLocationActivityReports, now, sevenDaysAgo);

        await SendTelegramMessage(telegramMessage);

        _logger.LogInformation("WeeklyEarthquakeAnalyser is finished");
    }

    private static string CreateWeeklyTelegramMessage(List<WeeklyLocationActivityReport> locationActivityReports, DateTime now, DateTime sevenDaysAgo)
    {
        StringBuilder sb = new StringBuilder();

        TimeZoneInfo turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

        DateTime turkeyStartDate = TimeZoneInfo.ConvertTimeFromUtc(sevenDaysAgo, turkeyTimeZone);
        
        DateTime turkeyEndDate = TimeZoneInfo.ConvertTimeFromUtc(now, turkeyTimeZone);

        sb.AppendLine("Haftalık Deprem Raporu");
        
        sb.AppendLine($"{turkeyStartDate:dd.MM.yyyy HH:mm} - {turkeyEndDate:dd.MM.yyyy HH:mm}");
        
        sb.AppendLine();

        foreach (WeeklyLocationActivityReport locationActivityReport in locationActivityReports)
        {
            sb.AppendLine($"📍 {locationActivityReport.Location}");

            sb.AppendLine($"Toplam: {locationActivityReport.TotalCount} Deprem");

            sb.AppendLine($"Max: {Math.Round(locationActivityReport.MaxMagnitude, 2)}");

            if (locationActivityReport.DailyStatistics.Any())
            {
                sb.AppendLine("Günlük Dağılım:");

                foreach (KeyValuePair<DateTime, DailyStatistic> day in locationActivityReport.DailyStatistics.OrderBy(x => x.Key))
                {
                    sb.AppendLine($"  {day.Key:dd.MM.yyyy} → {day.Value.Count} (Max {Math.Round(day.Value.MaxMagnitude, 2)})");
                }
            }

            if (locationActivityReport.MagnitudeDistribution.Any())
            {
                sb.AppendLine("Büyüklük Dağılımı:");

                foreach (KeyValuePair<double, int> mag in locationActivityReport.MagnitudeDistribution.OrderByDescending(x => x.Value))
                {
                    sb.Append($"{mag.Value} x {mag.Key:F1} / ");
                }
            }

            sb.AppendLine();
            sb.AppendLine(new string('-', 30));
        }

        return sb.ToString();
    }

    private async Task SendTelegramMessage(string alertMessage)
    {
        int chatId = await GetAdminTelegramChatId();

        SendMessageApiRequest sendMessageApiRequest = new SendMessageApiRequest
        {
            ChatId = chatId,
            Text = alertMessage
        };

        ProxyResponse<SendMessageApiResponse> sendMessageProxyResponse = await _telegramApiProxy.SendMessage(sendMessageApiRequest);

        if (sendMessageProxyResponse.HasError)
        {
            ProblemDetails problemDetails = sendMessageProxyResponse.ProblemDetails;

            if (problemDetails.Status is StatusCodes.Status500InternalServerError or StatusCodes.Status408RequestTimeout)
            {
                throw new Exception("A transient error occured while sending message");
            }

            throw new ApplicationException($"An error occured while sending message. Message: {alertMessage}");
        }

        SendMessageApiResponse sendMessageApiResponse = sendMessageProxyResponse.Data;

        if (sendMessageApiResponse.Ok is false)
        {
            throw new ApplicationException($"Telegram message could not be sent. Message: {alertMessage}");
        }
    }

    private async Task<int> GetAdminTelegramChatId()
    {
        const string key = ConfigurationKeys.AdminTelegramChatId;

        Configuration? adminTelegramChatIdConfiguration = await _configurationService.GetConfiguration(key);

        if (adminTelegramChatIdConfiguration is null)
        {
            throw new ApplicationException($"Configuration not found. Key: {key}");
        }

        if (int.TryParse(adminTelegramChatIdConfiguration.Value, out int adminTelegramChatId) is false)
        {
            throw new ApplicationException($"Configuration value could not be parsed to int. Key: {key}");
        }

        return adminTelegramChatId;
    }
}
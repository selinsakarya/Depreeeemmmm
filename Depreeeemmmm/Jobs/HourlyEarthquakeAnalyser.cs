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
public class HourlyEarthquakeAnalyser : IJob
{
    private readonly ILogger<HourlyEarthquakeAnalyser> _logger;
    private readonly DepremDbContext _depremDbContext;
    private readonly ITelegramApiProxy _telegramApiProxy;
    private readonly IConfigurationService _configurationService;

    public HourlyEarthquakeAnalyser(
        ILogger<HourlyEarthquakeAnalyser> logger, 
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
        _logger.LogInformation("HourlyEarthquakeAnalyser is started");
        
        DateTime now = DateTime.UtcNow;

        DateTime anHourAgo = now.AddHours(-1);

        List<Earthquake> earthquakes = await _depremDbContext.Earthquakes.AsNoTracking().Where(x => x.OccurredAt >= anHourAgo).ToListAsync();

        if (earthquakes.Count == 0)
        {
            _logger.LogInformation("No earthquakes found");

            return;
        }

        Dictionary<string, LocationActivityReport> result = earthquakes.ToLocationActivityReport(LocationActivityReportType.Hourly, now, anHourAgo);

        List<LocationActivityReport> locationActivityReports = result.Values
            .OrderByDescending(x => x.TotalCount)
            .Take(3)
            .ToList();
        
        string telegramMessage = locationActivityReports.ToTelegramMessage();

        await SendTelegramMessage(telegramMessage);
        
        _logger.LogInformation("HourlyEarthquakeAnalyser is finished");
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
using System.Text;
using Depreeeemmmm.Data;
using Depreeeemmmm.Data.Entities;
using Depreeeemmmm.Proxies;
using Depreeeemmmm.Proxies.TelegramApi;
using Depreeeemmmm.Proxies.TelegramApi.Models.Requests;
using Depreeeemmmm.Proxies.TelegramApi.Models.Responses;
using Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Depreeeemmmm.Consumers;

public class NotifyAdminWhenEarthquakeWhenEarthquakeOccured : IConsumer<EarthquakeOccurred>
{
    private readonly ILogger<NotifyAdminWhenEarthquakeWhenEarthquakeOccured> _logger;
    private readonly DepremDbContext _depremDbContext;
    private readonly ITelegramApiProxy _telegramApiProxy;

    public NotifyAdminWhenEarthquakeWhenEarthquakeOccured(
        ILogger<NotifyAdminWhenEarthquakeWhenEarthquakeOccured> logger,
        DepremDbContext depremDbContext, 
        ITelegramApiProxy telegramApiProxy)
    {
        _logger = logger;
        _depremDbContext = depremDbContext;
        _telegramApiProxy = telegramApiProxy;
    }

    public async Task Consume(ConsumeContext<EarthquakeOccurred> context)
    {
        EarthquakeOccurred earthquakeOccurredEvent = context.Message;

        _logger.LogInformation("NotifyAdminWhenEarthquakeWhenEarthquakeOccured is started. EarthquakeSecondaryUniqueId: {EarthquakeSecondaryUniqueId}", earthquakeOccurredEvent.EarthquakeSecondaryUniqueId);

        AdminLocation? adminLocation = await _depremDbContext.AdminLocations.AsNoTracking().OrderByDescending(a => a.CreatedAt).FirstOrDefaultAsync(context.CancellationToken);

        if (adminLocation is null)
        {
            _logger.LogWarning("AdminLocation is null");

            return;
        }
        
        Earthquake? earthquake = await _depremDbContext.Earthquakes.AsNoTracking().FirstOrDefaultAsync(e => e.SecondaryUniqueId == earthquakeOccurredEvent.EarthquakeSecondaryUniqueId, context.CancellationToken);

        if (earthquake is null)
        {
            _logger.LogWarning("Earthquake is null");

            return;
        }

        bool isEarthquakeOccurredNearAdmin = IsEarthquakeOccurredNearAdmin(earthquake, adminLocation);

        bool isMagnitudeAboveThreshold = IsMagnitudeAboveThreshold(earthquake, threshold: 3.9);

        bool isDepthBelowThreshold = IsDepthBelowThreshold(earthquake, threshold: 10);

        bool isMagnitudeJumpDetected = await IsMagnitudeJumpDetected(earthquake, distanceInKm: 50, lookbackHours: 48, minJump: 1.5);
        
        string alertMessage = BuildTelegramAlertMessage(earthquake, isEarthquakeOccurredNearAdmin, isMagnitudeAboveThreshold, isDepthBelowThreshold, isMagnitudeJumpDetected);

        await SendTelegramMessage(alertMessage);

        _logger.LogInformation("NotifyAdminWhenEarthquakeWhenEarthquakeOccured is finished. EarthquakeSecondaryUniqueId: {EarthquakeSecondaryUniqueId}", earthquakeOccurredEvent.EarthquakeSecondaryUniqueId);
    }

    private static bool IsEarthquakeOccurredNearAdmin(Earthquake earthquake, AdminLocation adminLocation)
    {
        double earthquakeLatitude = earthquake.Coordinates.Y;

        double earthquakeLongitude = earthquake.Coordinates.X;

        double distanceInKm = CalculateDistanceInKm(earthquakeLatitude, earthquakeLongitude, adminLocation.Latitude, adminLocation.longitude);

        const int maxDistanceInKm = 50;

        bool isEarthquakeOccurredNearAdmin = distanceInKm <= maxDistanceInKm;

        return isEarthquakeOccurredNearAdmin;
    }

    private static double CalculateDistanceInKm(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        double deltaLatitude = ToRadians(latitude2 - latitude1);
        double deltaLongitude = ToRadians(longitude2 - longitude1);

        double haversine = Math.Sin(deltaLatitude / 2) * Math.Sin(deltaLatitude / 2) +
                           Math.Cos(ToRadians(latitude1)) * Math.Cos(ToRadians(latitude2)) *
                           Math.Sin(deltaLongitude / 2) * Math.Sin(deltaLongitude / 2);

        double centralAngle = 2 * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1 - haversine));

        const double earthRadiusKm = 6371;

        return earthRadiusKm * centralAngle;
    }

    private static double ToRadians(double angle) => angle * Math.PI / 180.0;

    private static bool IsMagnitudeAboveThreshold(Earthquake earthquake, double threshold)
    {
        bool isMagnitudeAboveThreshold = earthquake.Magnitude > threshold;
        
        return isMagnitudeAboveThreshold;
    }

    private static bool IsDepthBelowThreshold(Earthquake earthquake, double threshold)
    {
        bool isDepthBelowThreshold =  earthquake.Depth < threshold;

        return isDepthBelowThreshold;
    }

    private async Task<bool> IsMagnitudeJumpDetected(Earthquake earthquake, int distanceInKm, int lookbackHours, double minJump)
    {
        DateTime lookbackStart = earthquake.OccurredAt.AddHours(-lookbackHours);

        List<Earthquake> recentEarthquakes = await _depremDbContext.Earthquakes.Where(e => e.OccurredAt >= lookbackStart && e.OccurredAt < earthquake.OccurredAt).ToListAsync();

        List<Earthquake> nearbyEarthquakes = new List<Earthquake>();

        foreach (Earthquake recentEarthquake in recentEarthquakes)
        {
            double distance = CalculateDistanceInKm(earthquake.Coordinates.Y, earthquake.Coordinates.X, recentEarthquake.Coordinates.Y, recentEarthquake.Coordinates.X);

            bool isRecentEarthquakeOccurredNearCurrentEarthquake = distance <= distanceInKm;

            if (isRecentEarthquakeOccurredNearCurrentEarthquake)
            {
                nearbyEarthquakes.Add(recentEarthquake);
            }
        }

        if (nearbyEarthquakes.Count == 0)
        {
            return false;
        }

        double maxRecentMagnitude = nearbyEarthquakes.Max(e => e.Magnitude);

        return earthquake.Magnitude >= maxRecentMagnitude + minJump;
    }

    private static string BuildTelegramAlertMessage(Earthquake earthquake, bool isEarthquakeOccurredNearAdmin, bool isMagnitudeAboveThreshold, bool isDepthBelowThreshold, bool isMagnitudeJumpDetected)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("🚨 *Deprem Uyarısı*");
        sb.AppendLine();
        sb.AppendLine($"📍 Konum: {earthquake.Location}");
        sb.AppendLine($"📏 Büyüklük: {earthquake.Magnitude}");
        sb.AppendLine($"📉 Derinlik: {earthquake.Depth} km");
        sb.AppendLine($"🕒 Zaman: {earthquake.OccurredAt:dd.MM.yyyy HH:mm} UTC");
        sb.AppendLine();

        sb.AppendLine("⚠️ *Tetiklenen Kriterler:*");

        if (isEarthquakeOccurredNearAdmin)
        {
            sb.AppendLine("• Admin lokasyonuna yakın");
        }

        if (isMagnitudeAboveThreshold)
        {
            sb.AppendLine("• Büyüklük eşik değerin üzerinde");
        }

        if (isDepthBelowThreshold)
        {
            sb.AppendLine("• Yüzeye yakın derinlikte");
        }

        if (isMagnitudeJumpDetected)
        {
            sb.AppendLine("• Bölgesel büyüklük sıçraması tespit edildi");
        }

        sb.AppendLine();

        string riskLevel = CalculateRiskLevel(isMagnitudeAboveThreshold, isDepthBelowThreshold, isMagnitudeJumpDetected);

        sb.AppendLine($"🔥 *Risk Seviyesi:* {riskLevel}");

        return sb.ToString();
    }
    
    private static string CalculateRiskLevel(bool isMagnitudeAboveThreshold, bool isDepthBelowThreshold, bool isMagnitudeJumpDetected)
    {
        int score = 0;

        if (isMagnitudeAboveThreshold)
        {
            score += 2;
        }

        if (isDepthBelowThreshold)
        {
            score += 2;
        }
        
        if (isMagnitudeJumpDetected)
        {
            score += 3;
        }

        return score switch
        {
            >= 5 => "YÜKSEK",
            >= 3 => "ORTA",
            _ => "BİLGİ"
        };
    }
    
    private async Task SendTelegramMessage(string alertMessage)
    {
        SendMessageApiRequest sendMessageApiRequest = new SendMessageApiRequest
        {
            ChatId = 1725466102,
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
}
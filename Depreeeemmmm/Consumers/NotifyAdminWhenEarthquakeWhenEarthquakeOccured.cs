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

        Task<bool> isMagnitudeJumpDetectedTask = IsMagnitudeJumpDetected(earthquake, maxDistanceInKm: 50, timeWindowInHours: 48, minJump: 1.5);

        Task<bool> isDepthTrendGoingUpwardTask = IsDepthTrendGoingUpward(earthquake, maxDistanceInKm: 50, timeWindowInHours: 48, minimumEarthQuakeToCompare: 5);

        Task<bool> isClusterDensityHighTask = IsClusterDensityHigh(earthquake, maxDistanceInKm: 50, timeWindowInHours: 48, minimumEarthQuakeCount: 15);

        await Task.WhenAll(isMagnitudeJumpDetectedTask, isDepthTrendGoingUpwardTask, isClusterDensityHighTask);
        
        string alertMessage = BuildTelegramAlertMessage(earthquake,
            isEarthquakeOccurredNearAdmin, 
            isMagnitudeAboveThreshold, 
            isDepthBelowThreshold,
            isMagnitudeJumpDetectedTask.Result,
            isDepthTrendGoingUpwardTask.Result,
            isClusterDensityHighTask.Result);

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

    private async Task<bool> IsMagnitudeJumpDetected(Earthquake earthquake, int maxDistanceInKm, int timeWindowInHours, double minJump)
    {
        DateTime lookBackStart = earthquake.OccurredAt.AddHours(-timeWindowInHours);

        List<Earthquake> recentEarthquakes = await _depremDbContext.Earthquakes.Where(e => e.OccurredAt >= lookBackStart && e.OccurredAt < earthquake.OccurredAt).ToListAsync();

        List<Earthquake> nearbyEarthquakes = new List<Earthquake>();

        foreach (Earthquake recentEarthquake in recentEarthquakes)
        {
            bool isRecentEarthquakeOccurredNearCurrentEarthquake = IsRecentEarthquakeOccurredNearCurrentEarthquake(earthquake, recentEarthquake, maxDistanceInKm);

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

    private async Task<bool> IsDepthTrendGoingUpward(Earthquake earthquake, int maxDistanceInKm, int timeWindowInHours, int minimumEarthQuakeToCompare)
    {
        DateTime start = earthquake.OccurredAt.AddHours(-timeWindowInHours);

        List<Earthquake> recentEarthquakes = await _depremDbContext.Earthquakes
            .AsNoTracking()
            .Where(e => e.OccurredAt >= start && e.OccurredAt < earthquake.OccurredAt)
            .ToListAsync();
        
        List<Earthquake> nearbyEarthquakes = new List<Earthquake>();
        
        foreach (Earthquake recentEarthquake in recentEarthquakes)
        {
            bool isRecentEarthquakeOccurredNearCurrentEarthquake = IsRecentEarthquakeOccurredNearCurrentEarthquake(earthquake, recentEarthquake, maxDistanceInKm);

            if (isRecentEarthquakeOccurredNearCurrentEarthquake)
            {
                nearbyEarthquakes.Add(recentEarthquake);
            }
        }

        if (nearbyEarthquakes.Count < minimumEarthQuakeToCompare)
        {
            return false;
        }

        int mid = nearbyEarthquakes.Count / 2;

        double firstHalfAvgDepth = nearbyEarthquakes
            .Take(mid)
            .Average(e => e.Depth);

        double secondHalfAvgDepth = nearbyEarthquakes
            .Skip(mid)
            .Average(e => e.Depth);

        return secondHalfAvgDepth < firstHalfAvgDepth;
    }

    private async Task<bool> IsClusterDensityHigh(Earthquake earthquake, int maxDistanceInKm, int timeWindowInHours, int minimumEarthQuakeCount)
    {
        DateTime start = earthquake.OccurredAt.AddHours(-timeWindowInHours);

        List<Earthquake> recentEarthquakes = await _depremDbContext.Earthquakes.AsNoTracking().Where(e => e.OccurredAt >= start && e.OccurredAt < earthquake.OccurredAt).ToListAsync();

        List<Earthquake> nearbyEarthquakes = new List<Earthquake>();

        foreach (Earthquake recentEarthquake in recentEarthquakes)
        {
            bool isRecentEarthquakeOccurredNearCurrentEarthquake = IsRecentEarthquakeOccurredNearCurrentEarthquake(earthquake, recentEarthquake, maxDistanceInKm);

            if (isRecentEarthquakeOccurredNearCurrentEarthquake)
            {
                nearbyEarthquakes.Add(recentEarthquake);
            }
        }

        if (nearbyEarthquakes.Count >= minimumEarthQuakeCount)
        {
            return true;
        }

        return false;
    }

    private static string BuildTelegramAlertMessage(Earthquake earthquake, bool isEarthquakeOccurredNearAdmin, bool isMagnitudeAboveThreshold, bool isDepthBelowThreshold, bool isMagnitudeJumpDetected, bool isDepthTrendGoingUpward, bool isClusterDensityHigh)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("🚨 *Depreeeemmmm*");
        sb.AppendLine();
        sb.AppendLine($"📍 Konum: {earthquake.Location}");
        sb.AppendLine($"📏 Büyüklük: {earthquake.Magnitude}");
        sb.AppendLine($"📉 Derinlik: {earthquake.Depth} km");
        sb.AppendLine($"🕒 Zaman: {earthquake.OccurredAt:dd.MM.yyyy HH:mm} UTC");
        sb.AppendLine();

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

        if (isDepthTrendGoingUpward)
        {
            sb.AppendLine("• Sarsıntılar giderek daha sığ seviyelerde oluşuyor");
        }

        sb.AppendLine();

        return sb.ToString();
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

    private static bool IsRecentEarthquakeOccurredNearCurrentEarthquake(Earthquake earthquake, Earthquake recentEarthquake, int maxDistanceInKm)
    {
        double distance = CalculateDistanceInKm(earthquake.Coordinates.Y, earthquake.Coordinates.X, recentEarthquake.Coordinates.Y, recentEarthquake.Coordinates.X);

        bool isRecentEarthquakeOccurredNearCurrentEarthquake = distance <= maxDistanceInKm;
        
        return isRecentEarthquakeOccurredNearCurrentEarthquake;
    }
}
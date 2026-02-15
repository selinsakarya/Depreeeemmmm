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

        List<AdminLocation> adminLocations = await _depremDbContext.AdminLocations.AsNoTracking().ToListAsync();

        if (adminLocations.Count == 0)
        {
            _logger.LogWarning("No admin location defined");

            return;
        }

        Earthquake? earthquake = await _depremDbContext.Earthquakes.AsNoTracking().FirstOrDefaultAsync(e => e.SecondaryUniqueId == earthquakeOccurredEvent.EarthquakeSecondaryUniqueId, context.CancellationToken);

        if (earthquake is null)
        {
            _logger.LogWarning("Earthquake is null");

            return;
        }

        foreach (AdminLocation adminLocation in adminLocations)
        {
            double distanceToAdminInKm = CalculateDistanceInKm(earthquake.Coordinates.Y, earthquake.Coordinates.X, adminLocation.Latitude, adminLocation.longitude);

            bool isEarthquakeOccurredNearAdmin = distanceToAdminInKm <= 150;

            if (isEarthquakeOccurredNearAdmin is false)
            {
                _logger.LogWarning($"Skipping notification because earthquake is not near admin location. DistanceToAdminInKm: {distanceToAdminInKm}");

                return;
            }

            bool isMagnitudeAboveThreshold = IsMagnitudeAboveThreshold(earthquake, threshold: 3.9);

            bool isDepthBelowThreshold = IsDepthBelowThreshold(earthquake, threshold: 10);

            bool isMagnitudeJumpDetected = await IsMagnitudeJumpDetected(earthquake, maxDistanceInKm: 150, timeWindowInHours: 48, minJump: 1.5);

            bool isDepthTrendGoingUpward = await IsDepthTrendGoingUpward(earthquake, maxDistanceInKm: 150, timeWindowInHours: 72, minimumEarthQuakeToCompare: 5);

            bool isClusterDensityHigh = await IsClusterDensityHigh(earthquake, maxDistanceInKm: 150, timeWindowInHours: 48, minimumEarthQuakeCount: 15);

            string alertMessage = BuildTelegramAlertMessage(earthquake,
                adminLocation,
                distanceToAdminInKm: distanceToAdminInKm,
                isEarthquakeOccurredNearAdmin: isEarthquakeOccurredNearAdmin,
                isMagnitudeAboveThreshold: isMagnitudeAboveThreshold,
                isDepthBelowThreshold: isDepthBelowThreshold,
                isMagnitudeJumpDetected: isMagnitudeJumpDetected,
                isDepthTrendGoingUpward: isDepthTrendGoingUpward,
                isClusterDensityHigh: isClusterDensityHigh);

            await SendTelegramMessage(alertMessage);
        }

        _logger.LogInformation("NotifyAdminWhenEarthquakeWhenEarthquakeOccured is finished. EarthquakeSecondaryUniqueId: {EarthquakeSecondaryUniqueId}", earthquakeOccurredEvent.EarthquakeSecondaryUniqueId);
    }

    private static bool IsMagnitudeAboveThreshold(Earthquake earthquake, double threshold)
    {
        bool isMagnitudeAboveThreshold = earthquake.Magnitude > threshold;

        return isMagnitudeAboveThreshold;
    }

    private static bool IsDepthBelowThreshold(Earthquake earthquake, double threshold)
    {
        bool isDepthBelowThreshold = earthquake.Depth < threshold;

        return isDepthBelowThreshold;
    }

    private async Task<bool> IsMagnitudeJumpDetected(Earthquake earthquake, int maxDistanceInKm, int timeWindowInHours, double minJump)
    {
        List<Earthquake> nearbyEarthquakes = await GetNearbyEarthquakes(earthquake, maxDistanceInKm, timeWindowInHours);

        if (nearbyEarthquakes.Count == 0)
        {
            return false;
        }

        double maxRecentMagnitude = nearbyEarthquakes.Max(e => e.Magnitude);

        return earthquake.Magnitude >= maxRecentMagnitude + minJump;
    }

    private async Task<bool> IsDepthTrendGoingUpward(Earthquake earthquake, int maxDistanceInKm, int timeWindowInHours, int minimumEarthQuakeToCompare)
    {
        List<Earthquake> nearbyEarthquakes = await GetNearbyEarthquakes(earthquake, maxDistanceInKm, timeWindowInHours);

        if (nearbyEarthquakes.Count < minimumEarthQuakeToCompare)
        {
            return false;
        }

        int mid = nearbyEarthquakes.Count / 2;

        double firstHalfAvgDepth = nearbyEarthquakes.Take(mid).Average(e => e.Depth);

        double secondHalfAvgDepth = nearbyEarthquakes.Skip(mid).Average(e => e.Depth);

        return secondHalfAvgDepth < firstHalfAvgDepth;
    }

    private async Task<bool> IsClusterDensityHigh(Earthquake earthquake, int maxDistanceInKm, int timeWindowInHours, int minimumEarthQuakeCount)
    {
        List<Earthquake> nearbyEarthquakes = await GetNearbyEarthquakes(earthquake, maxDistanceInKm, timeWindowInHours);

        if (nearbyEarthquakes.Count >= minimumEarthQuakeCount)
        {
            return true;
        }

        return false;
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

        double distance = earthRadiusKm * centralAngle;

        return Math.Round(distance, 2);
    }

    private static double ToRadians(double angle) => angle * Math.PI / 180.0;

    private async Task<List<Earthquake>> GetNearbyEarthquakes(Earthquake earthquake, int maxDistanceInKm, int timeWindowInHours)
    {
        DateTime start = earthquake.OccurredAt.AddHours(-timeWindowInHours);

        List<Earthquake> recentEarthquakes = await _depremDbContext.Earthquakes.Where(e => e.OccurredAt >= start && e.OccurredAt < earthquake.OccurredAt).ToListAsync();

        List<Earthquake> nearbyEarthquakes = new List<Earthquake>();

        foreach (Earthquake recentEarthquake in recentEarthquakes)
        {
            bool isRecentEarthquakeOccurredNearCurrentEarthquake = IsRecentEarthquakeOccurredNearCurrentEarthquake(earthquake, recentEarthquake, maxDistanceInKm);

            if (isRecentEarthquakeOccurredNearCurrentEarthquake)
            {
                nearbyEarthquakes.Add(recentEarthquake);
            }
        }

        return nearbyEarthquakes;
    }

    private static bool IsRecentEarthquakeOccurredNearCurrentEarthquake(Earthquake earthquake, Earthquake recentEarthquake, int maxDistanceInKm)
    {
        double distance = CalculateDistanceInKm(earthquake.Coordinates.Y, earthquake.Coordinates.X, recentEarthquake.Coordinates.Y, recentEarthquake.Coordinates.X);

        bool isRecentEarthquakeOccurredNearCurrentEarthquake = distance <= maxDistanceInKm;

        return isRecentEarthquakeOccurredNearCurrentEarthquake;
    }

    private static string BuildTelegramAlertMessage(Earthquake earthquake, AdminLocation adminLocation, double distanceToAdminInKm, bool isEarthquakeOccurredNearAdmin, bool isMagnitudeAboveThreshold, bool isDepthBelowThreshold, bool isMagnitudeJumpDetected, bool isDepthTrendGoingUpward, bool isClusterDensityHigh)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"📍Konum: {earthquake.Location}");
        sb.AppendLine($"📈Büyüklük: {earthquake.Magnitude}");
        sb.AppendLine($"📏Derinlik: {earthquake.Depth} km");

        TimeZoneInfo turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

        DateTime turkeyDateTime = TimeZoneInfo.ConvertTimeFromUtc(earthquake.OccurredAt, turkeyTimeZone);

        sb.AppendLine($"🕓Zaman: {turkeyDateTime:dd.MM.yyyy HH:mm} Turkey Time");
        sb.AppendLine();

        if (isEarthquakeOccurredNearAdmin)
        {
            sb.AppendLine($"🚨{adminLocation.Name} lokasyonuna yakın - {distanceToAdminInKm} km");
        }

        if (isMagnitudeJumpDetected)
        {
            sb.AppendLine("•🚨 Bölgesel büyüklük sıçraması tespit edildi");
        }

        if (isDepthTrendGoingUpward)
        {
            sb.AppendLine("•🚨 Sarsıntılar giderek daha sığ seviyelerde oluşuyor");
        }

        if (isMagnitudeAboveThreshold)
        {
            sb.AppendLine("⚠️ Büyüklük eşik değerin üzerinde");
        }

        if (isDepthBelowThreshold)
        {
            sb.AppendLine("⚠️ Yüzeye yakın derinlikte");
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
}
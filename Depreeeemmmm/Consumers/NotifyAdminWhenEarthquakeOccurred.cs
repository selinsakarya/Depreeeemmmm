using System.Text;
using Depreeeemmmm.Constants;
using Depreeeemmmm.Data;
using Depreeeemmmm.Data.Entities;
using Depreeeemmmm.Models;
using Depreeeemmmm.Proxies;
using Depreeeemmmm.Proxies.TelegramApiProxy;
using Depreeeemmmm.Proxies.TelegramApiProxy.Models.Requests;
using Depreeeemmmm.Proxies.TelegramApiProxy.Models.Responses;
using Depreeeemmmm.Services;
using Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Depreeeemmmm.Consumers;

public class NotifyAdminWhenEarthquakeOccurred : IConsumer<EarthquakeOccurred>
{
    private readonly ILogger<NotifyAdminWhenEarthquakeOccurred> _logger;
    private readonly DepremDbContext _depremDbContext;
    private readonly ITelegramApiProxy _telegramApiProxy;
    private readonly IConfigurationService _configurationService;

    public NotifyAdminWhenEarthquakeOccurred(
        ILogger<NotifyAdminWhenEarthquakeOccurred> logger,
        DepremDbContext depremDbContext,
        ITelegramApiProxy telegramApiProxy,
        IConfigurationService configurationService)
    {
        _logger = logger;
        _depremDbContext = depremDbContext;
        _telegramApiProxy = telegramApiProxy;
        _configurationService = configurationService;
    }

    public async Task Consume(ConsumeContext<EarthquakeOccurred> context)
    {
        EarthquakeOccurred earthquakeOccurredEvent = context.Message;

        _logger.LogInformation("NotifyAdminWhenEarthquakeOccurred is started. EarthquakeSecondaryUniqueId: {EarthquakeSecondaryUniqueId}", earthquakeOccurredEvent.EarthquakeSecondaryUniqueId);

        List<AdminLocation> adminLocations = await _depremDbContext.AdminLocations.AsNoTracking().ToListAsync();

        if (adminLocations.Count == 0)
        {
            _logger.LogWarning("No admin location defined");

            return;
        }

        Earthquake? earthquake = await _depremDbContext.Earthquakes.AsNoTracking().FirstOrDefaultAsync(e => e.SecondaryUniqueId == earthquakeOccurredEvent.EarthquakeSecondaryUniqueId, context.CancellationToken);

        if (earthquake is null)
        {
            throw new ApplicationException("Earthquake not found");
        }

        foreach (AdminLocation adminLocation in adminLocations)
        {
            double distanceToAdminInKm = CalculateDistanceInKm(earthquake.Coordinates.Y, earthquake.Coordinates.X, adminLocation.Latitude, adminLocation.longitude);

            bool isEarthquakeOccurredNearAdmin = distanceToAdminInKm <= 150;

            if (isEarthquakeOccurredNearAdmin is false)
            {
                _logger.LogWarning($"Skipping earthquake because it is not near admin location. DistanceToAdminInKm: {distanceToAdminInKm} AdminLocationName: {adminLocation.Name} Location: {earthquake.Location}");

                return;
            }

            bool isMagnitudeAboveThreshold = IsMagnitudeAboveThreshold(earthquake, threshold: 3.9);

            bool isDepthBelowThreshold = IsDepthBelowThreshold(earthquake, threshold: 10);

            List<Earthquake> nearbyEarthquakes = await GetNearbyEarthquakes(earthquake, maxDistanceInKm: 150, timeWindowInHours: 48);

            bool isMagnitudeJumpDetected = IsMagnitudeJumpDetected(earthquake, nearbyEarthquakes, minJump: 1.2);

            bool isDepthTrendGoingUpward = IsDepthTrendGoingUpward(nearbyEarthquakes, minimumEarthQuakeToCompare: 5);

            bool isMagnitudeTrendGoingUpward = IsMagnitudeTrendGoingUpward(nearbyEarthquakes, minimumEarthQuakeToCompare: 5);

            bool isClusterDensityHigh = IsClusterDensityHigh(nearbyEarthquakes, minimumEarthQuakeCount: 8);

            AdminEarthquakeAlertNotificationParameters adminEarthquakeAlertNotificationParameters = new AdminEarthquakeAlertNotificationParameters()
            {
                Earthquake = earthquake,
                AdminLocation = adminLocation,
                DistanceToAdminInKm = distanceToAdminInKm,
                IsEarthquakeOccurredNearAdmin = isEarthquakeOccurredNearAdmin,
                IsMagnitudeAboveThreshold = isMagnitudeAboveThreshold,
                IsDepthBelowThreshold = isDepthBelowThreshold,
                IsMagnitudeJumpDetected = isMagnitudeJumpDetected,
                IsDepthTrendGoingUpward = isDepthTrendGoingUpward,
                IsMagnitudeTrendGoingUpward = isMagnitudeTrendGoingUpward,
                IsClusterDensityHigh = isClusterDensityHigh
            };

            string alertMessage = BuildTelegramAlertMessage(adminEarthquakeAlertNotificationParameters);
            
            _logger.LogInformation(alertMessage);

            await SendTelegramMessage(alertMessage);
        }

        _logger.LogInformation("NotifyAdminWhenEarthquakeOccurred is finished. EarthquakeSecondaryUniqueId: {EarthquakeSecondaryUniqueId}", earthquakeOccurredEvent.EarthquakeSecondaryUniqueId);
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

    private static bool IsMagnitudeJumpDetected(Earthquake earthquake, List<Earthquake> nearbyEarthquakes, double minJump)
    {
        if (nearbyEarthquakes.Count == 0)
        {
            return false;
        }

        double maxRecentMagnitude = nearbyEarthquakes.Max(e => e.Magnitude);

        return earthquake.Magnitude >= maxRecentMagnitude + minJump;
    }

    private static bool IsDepthTrendGoingUpward(List<Earthquake> nearbyEarthquakes, int minimumEarthQuakeToCompare)
    {
        if (nearbyEarthquakes.Count < minimumEarthQuakeToCompare)
        {
            return false;
        }

        int mid = nearbyEarthquakes.Count / 2;

        double firstHalfAvgDepth = nearbyEarthquakes.Take(mid).Average(e => e.Depth);

        double secondHalfAvgDepth = nearbyEarthquakes.Skip(mid).Average(e => e.Depth);

        return secondHalfAvgDepth < firstHalfAvgDepth;
    }

    private static bool IsMagnitudeTrendGoingUpward(List<Earthquake> nearbyEarthquakes, int minimumEarthQuakeToCompare)
    {
        if (nearbyEarthquakes.Count < minimumEarthQuakeToCompare)
        {
            return false;
        }

        int upwardMovementsCount = 0;

        int totalComparisons = nearbyEarthquakes.Count - 1;

        for (int i = 1; i < nearbyEarthquakes.Count; i++)
        {
            if (nearbyEarthquakes[i].Magnitude >= nearbyEarthquakes[i - 1].Magnitude)
            {
                upwardMovementsCount += 1;
            }
        }

        double ratio = (double)upwardMovementsCount / totalComparisons;

        return ratio >= 0.6;
    }

    private static bool IsClusterDensityHigh(List<Earthquake> nearbyEarthquakes, int minimumEarthQuakeCount)
    {
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

        return nearbyEarthquakes.OrderByDescending(e => e.OccurredAt).ToList();
    }

    private static bool IsRecentEarthquakeOccurredNearCurrentEarthquake(Earthquake earthquake, Earthquake recentEarthquake, int maxDistanceInKm)
    {
        double distance = CalculateDistanceInKm(earthquake.Coordinates.Y, earthquake.Coordinates.X, recentEarthquake.Coordinates.Y, recentEarthquake.Coordinates.X);

        bool isRecentEarthquakeOccurredNearCurrentEarthquake = distance <= maxDistanceInKm;

        return isRecentEarthquakeOccurredNearCurrentEarthquake;
    }

    private static string BuildTelegramAlertMessage(AdminEarthquakeAlertNotificationParameters parameters)
    {
        string alertHeader = GetAlertHeader(parameters);

        StringBuilder sb = new StringBuilder();

        sb.AppendLine(alertHeader);

        TimeZoneInfo turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

        DateTime turkeyDateTime = TimeZoneInfo.ConvertTimeFromUtc(parameters.Earthquake.OccurredAt, turkeyTimeZone);

        sb.AppendLine($"Konum: {parameters.Earthquake.Location}, Büyüklük: {parameters.Earthquake.Magnitude}, Derinlik: {parameters.Earthquake.Depth} km, Zaman: {turkeyDateTime:dd.MM.yyyy HH:mm}");

        sb.AppendLine();

        if (parameters.IsEarthquakeOccurredNearAdmin)
        {
            sb.AppendLine($"⚠️{parameters.AdminLocation.Name} lokasyonuna {parameters.DistanceToAdminInKm} km mesafede");
        }

        if (parameters.IsMagnitudeJumpDetected)
        {
            sb.AppendLine("•⚠️ Bölgesel büyüklük sıçraması tespit edildi");
        }

        if (parameters.IsDepthTrendGoingUpward)
        {
            sb.AppendLine("⚠️ Sarsıntılar giderek daha sığ seviyelerde oluşuyor");
        }

        if (parameters.IsMagnitudeAboveThreshold)
        {
            sb.AppendLine("⚠️ Büyüklük eşik değerin üzerinde");
        }

        if (parameters.IsDepthBelowThreshold)
        {
            sb.AppendLine("⚠️ Yüzeye yakın derinlikte");
        }

        if (parameters.IsMagnitudeTrendGoingUpward)
        {
            sb.AppendLine("⚠️ Deprem büyüklük trendi yukarı yönlü");
        }

        return sb.ToString();
    }

    private static string GetAlertHeader(AdminEarthquakeAlertNotificationParameters parameters)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("🚨");

        if (parameters.IsMagnitudeAboveThreshold)
        {
            sb.Append("🚨");
        }

        if (parameters.IsDepthBelowThreshold)
        {
            sb.Append("🚨");
        }

        if (parameters.IsMagnitudeJumpDetected)
        {
            sb.Append("🚨");
        }

        if (parameters.IsDepthTrendGoingUpward)
        {
            sb.Append("🚨");
        }

        if (parameters.IsClusterDensityHigh)
        {
            sb.Append("🚨");
        }

        if (parameters.IsMagnitudeTrendGoingUpward)
        {
            sb.Append("🚨");
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

            if (problemDetails.Status is >= StatusCodes.Status500InternalServerError or >= StatusCodes.Status400BadRequest)
            {
                throw new Exception($"A transient error occured while sending message. Status: {problemDetails.Status} Title: {problemDetails.Title} Type: {problemDetails.Type} Detail: {problemDetails.Detail}");
            }

            throw new ApplicationException($"A transient error occured while sending message. Status: {problemDetails.Status} Title: {problemDetails.Title} Type: {problemDetails.Type} Detail: {problemDetails.Detail}");
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
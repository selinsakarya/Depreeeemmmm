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
    private const double SignificantMagnitudeThreshold = 4.0;
    
    private const double AftershockMagnitudeGap = 0.5;
    
    private const int AftershockWindowHours = 72;

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

            List<Earthquake> nearbyEarthquakes = await GetNearbyEarthquakes(earthquake, maxDistanceInKm: 150, timeWindowInHours: 48);

            bool isMagnitudeAboveThreshold = IsMagnitudeAboveThreshold(earthquake, threshold: 3.9);
            
            bool isDepthBelowThreshold = IsDepthBelowThreshold(earthquake, threshold: 10);
            
            (bool isMagnitudeJumpDetected, double magnitudeJumpAmount) = DetectMagnitudeJump(earthquake, nearbyEarthquakes, minJump: 1.0);
            
            (bool isDepthTrendGoingUpward, double depthTrendChangeKm) = DetectShallowingDepthTrend(nearbyEarthquakes, minimumEarthQuakeToCompare: 5);
            
            bool isMagnitudeTrendGoingUpward = IsMagnitudeTrendGoingUpward(nearbyEarthquakes, minimumEarthQuakeToCompare: 5);
            
            bool isClusterDensityHigh = IsClusterDensityHigh(nearbyEarthquakes, minimumEarthQuakeCount: 10);
            
            double estimatedFeltIntensity = EstimateFeltIntensity(earthquake.Magnitude, distanceToAdminInKm);
            
            (bool isAftershock, double? mainshockMagnitude, double? hoursSinceMainshock) = DetectAftershock(earthquake, nearbyEarthquakes);
            
            bool isForeshockPattern = DetectForeshockPattern(earthquake, nearbyEarthquakes);
            
            (bool isSwarmDetected, int swarmEarthquakeCount) = DetectSwarm(earthquake, nearbyEarthquakes);
            
            (bool isActivityAccelerating, double? activityAccelerationRatio) = DetectActivityAcceleration(earthquake, nearbyEarthquakes);
            
            (bool isRepeatingEpicenter, int repeatingEpicenterCount) = DetectRepeatingEpicenter(earthquake, nearbyEarthquakes);
            
            bool isDominantEnergyRelease = IsDominantEnergyRelease(earthquake, nearbyEarthquakes);
            
            double? hoursSinceLastSignificantEarthquake = GetHoursSinceLastSignificantEarthquake(earthquake, nearbyEarthquakes);

            AdminEarthquakeAlertNotificationParameters adminEarthquakeAlertNotificationParameters = new AdminEarthquakeAlertNotificationParameters()
            {
                Earthquake = earthquake,
                AdminLocation = adminLocation,
                DistanceToAdminInKm = distanceToAdminInKm,
                IsEarthquakeOccurredNearAdmin = isEarthquakeOccurredNearAdmin,
                IsMagnitudeAboveThreshold = isMagnitudeAboveThreshold,
                IsDepthBelowThreshold = isDepthBelowThreshold,
                IsMagnitudeJumpDetected = isMagnitudeJumpDetected,
                MagnitudeJumpAmount = magnitudeJumpAmount,
                IsDepthTrendGoingUpward = isDepthTrendGoingUpward,
                DepthTrendChangeKm = depthTrendChangeKm,
                IsMagnitudeTrendGoingUpward = isMagnitudeTrendGoingUpward,
                IsClusterDensityHigh = isClusterDensityHigh,
                NearbyEarthquakeCount = nearbyEarthquakes.Count,
                MaxNearbyMagnitude = nearbyEarthquakes.Count > 0 ? nearbyEarthquakes.Max(e => e.Magnitude) : null,
                AverageNearbyDepth = nearbyEarthquakes.Count > 0 ? Math.Round(nearbyEarthquakes.Average(e => e.Depth), 1) : null,
                EstimatedFeltIntensity = estimatedFeltIntensity,
                IsAftershock = isAftershock,
                MainshockMagnitude = mainshockMagnitude,
                HoursSinceMainshock = hoursSinceMainshock,
                IsForeshockPattern = isForeshockPattern,
                IsSwarmDetected = isSwarmDetected,
                SwarmEarthquakeCount = swarmEarthquakeCount,
                IsActivityAccelerating = isActivityAccelerating,
                ActivityAccelerationRatio = activityAccelerationRatio,
                IsRepeatingEpicenter = isRepeatingEpicenter,
                RepeatingEpicenterCount = repeatingEpicenterCount,
                IsDominantEnergyRelease = isDominantEnergyRelease,
                HoursSinceLastSignificantEarthquake = hoursSinceLastSignificantEarthquake,
                RiskLevel = CalculateRiskLevel(
                    earthquake,
                    distanceToAdminInKm,
                    isMagnitudeAboveThreshold,
                    isDepthBelowThreshold,
                    isMagnitudeJumpDetected,
                    isDepthTrendGoingUpward,
                    isClusterDensityHigh,
                    isMagnitudeTrendGoingUpward,
                    isAftershock,
                    isForeshockPattern,
                    isSwarmDetected,
                    isActivityAccelerating,
                    isDominantEnergyRelease,
                    estimatedFeltIntensity)
            };

            string alertMessage = BuildTelegramAlertMessage(adminEarthquakeAlertNotificationParameters);

            _logger.LogInformation(alertMessage);

            var now = DateTime.UtcNow;

            if (earthquake.OccurredAt.Date == now.Date)
            {
                await SendTelegramMessage(alertMessage);
            }
        }

        _logger.LogInformation("NotifyAdminWhenEarthquakeOccurred is finished. EarthquakeSecondaryUniqueId: {EarthquakeSecondaryUniqueId}", earthquakeOccurredEvent.EarthquakeSecondaryUniqueId);
    }

    private static bool IsMagnitudeAboveThreshold(Earthquake earthquake, double threshold)
    {
        return earthquake.Magnitude > threshold;
    }

    private static bool IsDepthBelowThreshold(Earthquake earthquake, double threshold)
    {
        return earthquake.Depth < threshold;
    }

    private static (bool IsDetected, double JumpAmount) DetectMagnitudeJump(Earthquake earthquake, List<Earthquake> nearbyEarthquakes, double minJump)
    {
        if (nearbyEarthquakes.Count == 0)
        {
            return (false, 0);
        }

        double maxRecentMagnitude = nearbyEarthquakes.Max(e => e.Magnitude);
        double jumpAmount = Math.Round(earthquake.Magnitude - maxRecentMagnitude, 2);

        return (jumpAmount >= minJump, jumpAmount);
    }

    private static (bool IsShallowing, double DepthChangeKm) DetectShallowingDepthTrend(List<Earthquake> nearbyEarthquakes, int minimumEarthQuakeToCompare)
    {
        if (nearbyEarthquakes.Count < minimumEarthQuakeToCompare)
        {
            return (false, 0);
        }

        List<Earthquake> chronological = nearbyEarthquakes.OrderBy(e => e.OccurredAt).ToList();

        int mid = chronological.Count / 2;

        double firstHalfAvgDepth = chronological.Take(mid).Average(e => e.Depth);
        double secondHalfAvgDepth = chronological.Skip(mid).Average(e => e.Depth);
        double depthChangeKm = Math.Round(firstHalfAvgDepth - secondHalfAvgDepth, 1);

        return (depthChangeKm >= 2.0, depthChangeKm);
    }

    private static bool IsMagnitudeTrendGoingUpward(List<Earthquake> nearbyEarthquakes, int minimumEarthQuakeToCompare)
    {
        if (nearbyEarthquakes.Count < minimumEarthQuakeToCompare)
        {
            return false;
        }

        List<Earthquake> chronological = nearbyEarthquakes.OrderBy(e => e.OccurredAt).ToList();

        int upwardMovementsCount = 0;
        int totalComparisons = chronological.Count - 1;

        for (int i = 1; i < chronological.Count; i++)
        {
            if (chronological[i].Magnitude >= chronological[i - 1].Magnitude)
            {
                upwardMovementsCount += 1;
            }
        }

        double ratio = (double)upwardMovementsCount / totalComparisons;

        return ratio >= 0.6;
    }

    private static bool IsClusterDensityHigh(List<Earthquake> nearbyEarthquakes, int minimumEarthQuakeCount)
    {
        return nearbyEarthquakes.Count >= minimumEarthQuakeCount;
    }

    private static double EstimateFeltIntensity(double magnitude, double distanceInKm)
    {
        double safeDistance = Math.Max(distanceInKm, 1);

        double estimatedIntensity = 1.5 * magnitude - 3.5 * Math.Log10(safeDistance) + 1.0;

        return Math.Round(Math.Clamp(estimatedIntensity, 1, 10), 1);
    }

    private static (bool IsAftershock, double? MainshockMagnitude, double? HoursSinceMainshock) DetectAftershock(Earthquake earthquake, List<Earthquake> nearbyEarthquakes)
    {
        DateTime aftershockWindowStart = earthquake.OccurredAt.AddHours(-AftershockWindowHours);

        Earthquake? mainshock = nearbyEarthquakes
            .Where(e => e.OccurredAt >= aftershockWindowStart && e.Magnitude >= earthquake.Magnitude + AftershockMagnitudeGap)
            .OrderByDescending(e => e.Magnitude)
            .ThenByDescending(e => e.OccurredAt)
            .FirstOrDefault();

        if (mainshock is null)
        {
            return (false, null, null);
        }

        double hoursSinceMainshock = Math.Round((earthquake.OccurredAt - mainshock.OccurredAt).TotalHours, 1);

        return (true, mainshock.Magnitude, hoursSinceMainshock);
    }

    private static bool DetectForeshockPattern(Earthquake earthquake, List<Earthquake> nearbyEarthquakes)
    {
        List<Earthquake> recentChronological = nearbyEarthquakes
            .Where(e => e.OccurredAt >= earthquake.OccurredAt.AddHours(-24))
            .OrderBy(e => e.OccurredAt)
            .TakeLast(5)
            .ToList();

        if (recentChronological.Count < 4)
        {
            return false;
        }

        bool isIncreasingSequence = true;

        for (int i = 1; i < recentChronological.Count; i++)
        {
            if (recentChronological[i].Magnitude <= recentChronological[i - 1].Magnitude)
            {
                isIncreasingSequence = false;

                break;
            }
        }

        bool isCurrentLargest = earthquake.Magnitude > recentChronological.Max(e => e.Magnitude);

        return isIncreasingSequence && isCurrentLargest;
    }

    private static (bool IsSwarm, int SwarmCount) DetectSwarm(Earthquake earthquake, List<Earthquake> nearbyEarthquakes)
    {
        DateTime swarmWindowStart = earthquake.OccurredAt.AddHours(-24);

        List<Earthquake> swarmCandidates = nearbyEarthquakes
            .Where(e => e.OccurredAt >= swarmWindowStart)
            .Where(e => CalculateDistanceInKm(earthquake.Coordinates.Y, earthquake.Coordinates.X, e.Coordinates.Y, e.Coordinates.X) <= 50)
            .ToList();

        if (swarmCandidates.Count < 12)
        {
            return (false, swarmCandidates.Count);
        }

        double averageMagnitude = swarmCandidates.Average(e => e.Magnitude);

        return (averageMagnitude < 3.2, swarmCandidates.Count);
    }

    private static (bool IsAccelerating, double? AccelerationRatio) DetectActivityAcceleration(Earthquake earthquake, List<Earthquake> nearbyEarthquakes)
    {
        DateTime last24HoursStart = earthquake.OccurredAt.AddHours(-24);
        DateTime previous24HoursStart = earthquake.OccurredAt.AddHours(-48);

        int last24HoursCount = nearbyEarthquakes.Count(e => e.OccurredAt >= last24HoursStart);
        int previous24HoursCount = nearbyEarthquakes.Count(e => e.OccurredAt >= previous24HoursStart && e.OccurredAt < last24HoursStart);

        if (previous24HoursCount == 0)
        {
            return (last24HoursCount >= 8, null);
        }

        double accelerationRatio = Math.Round((double)last24HoursCount / previous24HoursCount, 1);

        return (accelerationRatio >= 2.0 && last24HoursCount >= 5, accelerationRatio);
    }

    private static (bool IsRepeating, int Count) DetectRepeatingEpicenter(Earthquake earthquake, List<Earthquake> nearbyEarthquakes)
    {
        const double epicenterRadiusKm = 10;

        int repeatingCount = nearbyEarthquakes.Count(e =>
            CalculateDistanceInKm(earthquake.Coordinates.Y, earthquake.Coordinates.X, e.Coordinates.Y, e.Coordinates.X) <= epicenterRadiusKm);

        return (repeatingCount >= 3, repeatingCount);
    }

    private static bool IsDominantEnergyRelease(Earthquake earthquake, List<Earthquake> nearbyEarthquakes)
    {
        if (nearbyEarthquakes.Count == 0)
        {
            return earthquake.Magnitude >= SignificantMagnitudeThreshold;
        }

        double currentEnergy = CalculateSeismicEnergy(earthquake.Magnitude);
        double previousEnergySum = nearbyEarthquakes.Sum(e => CalculateSeismicEnergy(e.Magnitude));

        return currentEnergy > previousEnergySum;
    }

    private static double CalculateSeismicEnergy(double magnitude)
    {
        return Math.Pow(10, 1.5 * magnitude);
    }

    private static double? GetHoursSinceLastSignificantEarthquake(Earthquake earthquake, List<Earthquake> nearbyEarthquakes)
    {
        Earthquake? lastSignificant = nearbyEarthquakes
            .Where(e => e.Magnitude >= SignificantMagnitudeThreshold)
            .OrderByDescending(e => e.OccurredAt)
            .FirstOrDefault();

        if (lastSignificant is null)
        {
            return null;
        }

        return Math.Round((earthquake.OccurredAt - lastSignificant.OccurredAt).TotalHours, 1);
    }

    private static string CalculateRiskLevel(
        Earthquake earthquake,
        double distanceToAdminInKm,
        bool isMagnitudeAboveThreshold,
        bool isDepthBelowThreshold,
        bool isMagnitudeJumpDetected,
        bool isDepthTrendGoingUpward,
        bool isClusterDensityHigh,
        bool isMagnitudeTrendGoingUpward,
        bool isAftershock,
        bool isForeshockPattern,
        bool isSwarmDetected,
        bool isActivityAccelerating,
        bool isDominantEnergyRelease,
        double estimatedFeltIntensity)
    {
        int warningScore = 0;

        if (isMagnitudeAboveThreshold) warningScore += 2;
        if (isDepthBelowThreshold) warningScore += 2;
        if (isMagnitudeJumpDetected) warningScore += 2;
        if (isDepthTrendGoingUpward) warningScore += 1;
        if (isClusterDensityHigh) warningScore += 1;
        if (isMagnitudeTrendGoingUpward) warningScore += 1;
        if (isForeshockPattern) warningScore += 3;
        if (isSwarmDetected) warningScore += 1;
        if (isActivityAccelerating) warningScore += 2;
        if (isDominantEnergyRelease) warningScore += 2;

        if (earthquake.Magnitude >= 5.0 || (earthquake.Magnitude >= 4.5 && distanceToAdminInKm <= 50 && isDepthBelowThreshold))
        {
            return "Kritik";
        }

        if (isForeshockPattern || warningScore >= 7 || (estimatedFeltIntensity >= 5 && earthquake.Magnitude >= 4.0))
        {
            return "Yüksek";
        }

        if (isAftershock && earthquake.Magnitude >= 4.0)
        {
            return "Yüksek";
        }

        if (warningScore >= 4 || estimatedFeltIntensity >= 4)
        {
            return "Orta";
        }

        return "Düşük";
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

        return distance <= maxDistanceInKm;
    }

    private static string BuildTelegramAlertMessage(AdminEarthquakeAlertNotificationParameters parameters)
    {
        string alertHeader = GetAlertHeader(parameters);

        StringBuilder sb = new StringBuilder();

        sb.AppendLine(alertHeader);
        sb.AppendLine($"Risk Seviyesi: {GetRiskLevelEmoji(parameters.RiskLevel)} {parameters.RiskLevel}");
        sb.AppendLine();

        TimeZoneInfo turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

        DateTime turkeyDateTime = TimeZoneInfo.ConvertTimeFromUtc(parameters.Earthquake.OccurredAt, turkeyTimeZone);

        sb.AppendLine($"Konum: {parameters.Earthquake.Location}");
        sb.AppendLine($"Büyüklük: {parameters.Earthquake.Magnitude} | Derinlik: {parameters.Earthquake.Depth} km | Zaman: {turkeyDateTime:dd.MM.yyyy HH:mm}");
        sb.AppendLine($"Tahmini hissedilme ({parameters.AdminLocation.Name}): MMI ~{parameters.EstimatedFeltIntensity} ({DescribeFeltIntensity(parameters.EstimatedFeltIntensity)})");
        sb.AppendLine($"Mesafe ({parameters.AdminLocation.Name}): {parameters.DistanceToAdminInKm} km");
        sb.AppendLine();

        sb.AppendLine("📊 Bölgesel Aktivite (48s / 150km):");
        sb.AppendLine($"• Toplam: {parameters.NearbyEarthquakeCount} deprem");

        if (parameters.MaxNearbyMagnitude.HasValue)
        {
            sb.AppendLine($"• En yüksek: {parameters.MaxNearbyMagnitude:F1} | Ort. derinlik: {parameters.AverageNearbyDepth:F1} km");
        }

        if (parameters.HoursSinceLastSignificantEarthquake.HasValue)
        {
            sb.AppendLine($"• Son M{SignificantMagnitudeThreshold}+ depremden bu yana: {parameters.HoursSinceLastSignificantEarthquake:F1} saat");
        }

        sb.AppendLine();
        sb.AppendLine("🔍 Analiz Sonuçları:");

        if (parameters.IsAftershock)
        {
            sb.AppendLine($"• Artçı deprem — ana deprem M{parameters.MainshockMagnitude:F1}, {parameters.HoursSinceMainshock:F1} saat önce");
        }

        if (parameters.IsForeshockPattern)
        {
            sb.AppendLine("• Öncül deprem dizisi — büyüklük art arda yükselerek ana depreme işaret edebilir");
        }

        if (parameters.IsMagnitudeJumpDetected)
        {
            sb.AppendLine($"• Büyüklük sıçraması: +{parameters.MagnitudeJumpAmount:F1} (bölgedeki son maksimuma göre)");
        }

        if (parameters.IsDepthTrendGoingUpward)
        {
            sb.AppendLine($"• Sığlaşma trendi: ortalama derinlik {parameters.DepthTrendChangeKm:F1} km azaldı");
        }

        if (parameters.IsMagnitudeTrendGoingUpward)
        {
            sb.AppendLine("• Büyüklük trendi yukarı yönlü (son depremlerin %60+ artış gösteriyor)");
        }

        if (parameters.IsClusterDensityHigh)
        {
            sb.AppendLine($"• Yoğun küme aktivitesi: {parameters.NearbyEarthquakeCount} deprem (48s pencerede)");
        }

        if (parameters.IsSwarmDetected)
        {
            sb.AppendLine($"• Sürü (swarm) aktivitesi: son 24s'de 50km içinde {parameters.SwarmEarthquakeCount} küçük deprem");
        }

        if (parameters.IsActivityAccelerating)
        {
            string ratioText = parameters.ActivityAccelerationRatio.HasValue
                ? $"{parameters.ActivityAccelerationRatio:F1}x"
                : "hızlandı";

            sb.AppendLine($"• Aktivite hızlanıyor: son 24s / önceki 24s oranı {ratioText}");
        }

        if (parameters.IsRepeatingEpicenter)
        {
            sb.AppendLine($"• Tekrarlayan odak: 10km yarıçapında {parameters.RepeatingEpicenterCount} deprem (aynı fay segmenti aktif olabilir)");
        }

        if (parameters.IsDominantEnergyRelease)
        {
            sb.AppendLine("• Enerji baskınlığı: bu deprem, bölgedeki önceki tüm depremlerden daha fazla enerji salıyor");
        }

        if (parameters.IsMagnitudeAboveThreshold)
        {
            sb.AppendLine("• Büyüklük eşik değerin üzerinde (M > 3.9)");
        }

        if (parameters.IsDepthBelowThreshold)
        {
            sb.AppendLine($"• Yüzeye yakın deprem (derinlik < 10 km) — yerel hasar riski daha yüksek");
        }

        if (!HasAnyAnalysisFlag(parameters))
        {
            sb.AppendLine("• Belirgin anomali tespit edilmedi, rutin izleme");
        }

        return sb.ToString();
    }

    private static bool HasAnyAnalysisFlag(AdminEarthquakeAlertNotificationParameters parameters)
    {
        return parameters.IsAftershock
               || parameters.IsForeshockPattern
               || parameters.IsMagnitudeJumpDetected
               || parameters.IsDepthTrendGoingUpward
               || parameters.IsMagnitudeTrendGoingUpward
               || parameters.IsClusterDensityHigh
               || parameters.IsSwarmDetected
               || parameters.IsActivityAccelerating
               || parameters.IsRepeatingEpicenter
               || parameters.IsDominantEnergyRelease
               || parameters.IsMagnitudeAboveThreshold
               || parameters.IsDepthBelowThreshold;
    }

    private static string DescribeFeltIntensity(double intensity)
    {
        return intensity switch
        {
            < 2 => "hissedilmez",
            < 3 => "zayıf",
            < 4 => "hafif",
            < 5 => "orta",
            < 6 => "güçlü",
            < 7 => "çok güçlü",
            _ => "şiddetli"
        };
    }

    private static string GetRiskLevelEmoji(string riskLevel)
    {
        return riskLevel switch
        {
            "Kritik" => "🔴",
            "Yüksek" => "🟠",
            "Orta" => "🟡",
            _ => "🟢"
        };
    }

    private static string GetAlertHeader(AdminEarthquakeAlertNotificationParameters parameters)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("🚨");

        if (parameters.RiskLevel is "Kritik" or "Yüksek")
        {
            sb.Append("🚨");
        }

        if (parameters.RiskLevel == "Kritik")
        {
            sb.Append("🚨");
        }

        if (parameters.IsForeshockPattern)
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
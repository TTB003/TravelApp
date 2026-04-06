using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;
using Microsoft.Extensions.Logging;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public class TravelBootstrapService : ITravelBootstrapService
{
    private const double NearbyRadiusMeters = 1500;
    private const double CacheReuseDistanceMeters = 200;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    private readonly IPoiApiClient _poiApiClient;
    private readonly ILocalDatabaseService _localDatabase;
    private readonly ILocationProvider _locationProvider;
    private readonly ITravelRuntimePipeline _travelRuntimePipeline;
    private readonly IPoiApiService _poiApiService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TravelBootstrapService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private bool _isStarted;
    private IReadOnlyList<PoiDto>? _cachedPois;
    private LocationSample? _cachedLocation;
    private DateTimeOffset? _cachedAtUtc;
    private string? _cachedLanguage;

    public TravelBootstrapService(
        IPoiApiClient poiApiClient,
        IPoiApiService poiApiService,
        ILocationProvider locationProvider,
        ITravelRuntimePipeline travelRuntimePipeline,
        ILocalDatabaseService localDatabase,
        TimeProvider timeProvider,
        ILogger<TravelBootstrapService> logger)
    {
        _poiApiClient = poiApiClient;
        _poiApiService = poiApiService;
        _locationProvider = locationProvider;
        _localDatabase = localDatabase;
        _travelRuntimePipeline = travelRuntimePipeline;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_isStarted)
            {
                _logger.LogDebug("Travel bootstrap: start ignored because pipeline is already running.");
                return;
            }

            var location = await _locationProvider.GetCurrentLocationAsync(cancellationToken);
            if (location is null)
            {
                _logger.LogWarning("Travel bootstrap: unable to start runtime pipeline because GPS location is unavailable.");
                return;
            }

            var languageCode = UserProfileService.PreferredLanguage;
            var pois = await GetNearbyPoisAsync(location, languageCode, cancellationToken);

            await _travelRuntimePipeline.StartAsync(pois, cancellationToken);
            _isStarted = true;
            _logger.LogInformation("Travel bootstrap: started runtime with {PoiCount} nearby POIs.", pois.Count);

            // Kick off background sync to refresh local cache from API
            _ = Task.Run(async () =>
            {
                try
                {
                    await _poiApiService.GetPoisAsync(location.Latitude, location.Longitude, NearbyRadiusMeters, languageCode, 1, 200, CancellationToken.None);
                    _logger.LogInformation("Travel bootstrap: background cache sync complete.");
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Travel bootstrap: background cache sync failed.");
                }
            });
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!_isStarted)
            {
                return;
            }

            await _travelRuntimePipeline.StopAsync(cancellationToken);
            _isStarted = false;
            _logger.LogInformation("Travel bootstrap: stopped runtime pipeline.");
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<PoiDto>> GetNearbyPoisAsync(LocationSample location, string? languageCode, CancellationToken cancellationToken)
    {
        if (CanReuseCache(location, languageCode))
        {
            _logger.LogDebug("Travel bootstrap: reused cached nearby POIs ({PoiCount}).", _cachedPois!.Count);
            return _cachedPois!;
        }

        IReadOnlyList<PoiDto> pois;
        try
        {
            var query = new NearbyPoiQueryDto(location.Latitude, location.Longitude, NearbyRadiusMeters);
            pois = await _poiApiClient.GetNearbyAsync(query, languageCode, cancellationToken);

            // Save snapshot of nearby POIs to local database for offline use
            try
            {
                var mobilePois = pois.Select(MapToMobilePoi).ToList();
                await _localDatabase.SavePoisAsync(mobilePois, CancellationToken.None);
                _logger.LogDebug("Travel bootstrap: saved {Count} nearby POIs to local cache.", mobilePois.Count);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Travel bootstrap: failed to save nearby POIs to local cache.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Travel bootstrap: failed to fetch nearby POIs from API.");

            // Try to load last-known nearby POIs from local cache when offline
            try
            {
                var offline = await _localDatabase.GetPoisAsync(languageCode, location.Latitude, location.Longitude, NearbyRadiusMeters, cancellationToken);
                if (offline is not null && offline.Count > 0)
                {
                    pois = offline.Select(MapFromMobilePoi).ToList();
                    _logger.LogInformation("Travel bootstrap: loaded {Count} nearby POIs from local cache.", pois.Count);
                }
                else
                {
                    pois = _cachedPois ?? [];
                }
            }
            catch (Exception dbEx)
            {
                _logger.LogDebug(dbEx, "Travel bootstrap: failed to read nearby POIs from local cache.");
                pois = _cachedPois ?? [];
            }
        }

        if (pois.Count == 0)
        {
            pois = new[] { CreateDemoPoiNear(location, languageCode) };
            _logger.LogInformation("Travel bootstrap: using demo POI fallback for runtime flow.");
        }

        _cachedPois = pois;
        _cachedLocation = location;
        _cachedAtUtc = _timeProvider.GetUtcNow();
        _cachedLanguage = languageCode;

        return pois;

    }

    private static TravelApp.Models.Contracts.PoiMobileDto MapToMobilePoi(PoiDto src)
    {
        return new TravelApp.Models.Contracts.PoiMobileDto
        {
            Id = src.Id,
            Title = src.Title,
            Subtitle = src.Subtitle,
            Description = src.Description ?? string.Empty,
            LanguageCode = src.PrimaryLanguage ?? "en",
            PrimaryLanguage = src.PrimaryLanguage ?? "en",
            ImageUrl = src.ImageUrl ?? string.Empty,
            Location = src.Location ?? string.Empty,
            Latitude = src.Latitude,
            Longitude = src.Longitude,
            GeofenceRadiusMeters = src.GeofenceRadiusMeters ?? 100,
            Category = src.Category ?? string.Empty,
            AudioAssets = src.AudioAssets?.Select(a => new TravelApp.Models.Contracts.PoiAudioMobileDto
            {
                Id = 0,
                LanguageCode = a.LanguageCode ?? "en",
                AudioUrl = a.AudioUrl,
                Transcript = a.Transcript,
                IsGenerated = a.IsGenerated
            }).ToList() ?? new List<TravelApp.Models.Contracts.PoiAudioMobileDto>()
        };
    }

    private static PoiDto MapFromMobilePoi(TravelApp.Models.Contracts.PoiMobileDto src)
    {
        return new PoiDto
        {
            Id = src.Id,
            Title = src.Title,
            Subtitle = src.Subtitle,
            ImageUrl = src.ImageUrl,
            Location = src.Location,
            Latitude = src.Latitude,
            Longitude = src.Longitude,
            GeofenceRadiusMeters = src.GeofenceRadiusMeters,
            PrimaryLanguage = src.PrimaryLanguage,
            Description = src.Description,
            Category = src.Category,
            AudioAssets = src.AudioAssets?.Select(a => new PoiAudioDto(a.LanguageCode, a.AudioUrl, a.Transcript, a.IsGenerated)).ToList() ?? new List<PoiAudioDto>()
        };
    }



    private bool CanReuseCache(LocationSample location, string? languageCode)
    {
        if (_cachedPois is null || _cachedLocation is null || !_cachedAtUtc.HasValue)
        {
            return false;
        }

        if (!string.Equals(_cachedLanguage, languageCode, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var age = _timeProvider.GetUtcNow() - _cachedAtUtc.Value;
        if (age > CacheTtl)
        {
            return false;
        }

        var distance = CalculateDistanceMeters(
            _cachedLocation.Latitude,
            _cachedLocation.Longitude,
            location.Latitude,
            location.Longitude);

        return distance <= CacheReuseDistanceMeters;
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000;
        static double ToRadians(double value) => value * Math.PI / 180;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }

    private static PoiDto CreateDemoPoiNear(LocationSample location, string? languageCode)
    {
        var lang = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode;

        return new PoiDto
        {
            Id = 999001,
            Title = "Demo POI - Nearby Audio Point",
            Subtitle = "Demo route",
            ImageUrl = "https://images.unsplash.com/photo-1480714378408-67cf0d13bc1f?w=1200&h=500&fit=crop",
            Location = "Demo location",
            Latitude = location.Latitude + 0.0001,
            Longitude = location.Longitude + 0.0001,
            GeofenceRadiusMeters = 200,
            PrimaryLanguage = lang,
            AudioAssets = []
        };
    }
}

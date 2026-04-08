using Microsoft.EntityFrameworkCore;
using TravelApp.Application.Abstractions.Pois;
using TravelApp.Application.Dtos.Pois;
using TravelApp.Domain.Entities;
using TravelApp.Infrastructure.Persistence;

namespace TravelApp.Infrastructure.Services.Pois;

public class PoiQueryService : IPoiQueryService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private const double EarthRadiusMeters = 6371000;

    private readonly TravelAppDbContext _dbContext;

    public PoiQueryService(TravelAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResultDto<PoiMobileDto>> GetAllAsync(PoiQueryRequestDto request, CancellationToken cancellationToken = default)
    {
        var languageCode = request.LanguageCode;
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);

        var hasGeoFilter = HasGeoFilter(request);
        Dictionary<int, double>? distanceByPoiId = null;
        List<int> pagedPoiIds;
        int totalCount;

        if (hasGeoFilter)
        {
            var lat = request.Latitude!.Value;
            var lng = request.Longitude!.Value;
            var radiusMeters = request.RadiusMeters!.Value;

            var latDelta = radiusMeters / 111320d;
            var safeCos = Math.Max(0.01, Math.Abs(Math.Cos(ToRadians(lat))));
            var lngDelta = radiusMeters / (111320d * safeCos);

            var minLat = lat - latDelta;
            var maxLat = lat + latDelta;
            var minLng = lng - lngDelta;
            var maxLng = lng + lngDelta;

            var candidates = await _dbContext.Pois
                .AsNoTracking()
                .Where(x => x.Latitude >= minLat
                            && x.Latitude <= maxLat
                            && x.Longitude >= minLng
                            && x.Longitude <= maxLng)
                .Select(x => new
                {
                    x.Id,
                    x.Latitude,
                    x.Longitude
                })
                .ToListAsync(cancellationToken);

            var filtered = candidates
                .Select(x => new
                {
                    x.Id,
                    Distance = CalculateHaversineDistanceMeters(lat, lng, x.Latitude, x.Longitude)
                })
                .Where(x => x.Distance <= radiusMeters)
                .OrderBy(x => x.Distance)
                .ThenBy(x => x.Id)
                .ToList();

            totalCount = filtered.Count;
            distanceByPoiId = filtered.ToDictionary(x => x.Id, x => x.Distance);

            pagedPoiIds = filtered
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Id)
                .ToList();
        }
        else
        {
            var query = _dbContext.Pois.AsNoTracking();
            totalCount = await query.CountAsync(cancellationToken);

            pagedPoiIds = await query
                .OrderBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        if (pagedPoiIds.Count == 0)
        {
            return new PagedResultDto<PoiMobileDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = []
            };
        }

        var pois = await _dbContext.Pois
            .AsNoTracking()
            .Where(x => pagedPoiIds.Contains(x.Id))
            .Include(x => x.Localizations)
            .Include(x => x.AudioAssets)
            .ToListAsync(cancellationToken);

        var orderMap = pagedPoiIds.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);

        var items = pois
            .Select(x => MapToMobileDto(x, languageCode, distanceByPoiId))
            .OrderBy(x => orderMap[x.Id])
            .ToList();

        return new PagedResultDto<PoiMobileDto>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<PoiMobileDto?> GetByIdAsync(int id, string? languageCode, CancellationToken cancellationToken = default)
    {
        var poi = await _dbContext.Pois
            .AsNoTracking()
            .Include(x => x.Localizations)
            .Include(x => x.AudioAssets)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return poi is null ? null : MapToMobileDto(poi, languageCode);
    }

    public async Task<PoiMobileDto> CreateAsync(UpsertPoiRequestDto request, CancellationToken cancellationToken = default)
    {
        var poi = new Poi();
        ApplyRequest(poi, request);

        _dbContext.Pois.Add(poi);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToMobileDto(poi, request.PrimaryLanguage);
    }

    public async Task<bool> UpdateAsync(int id, UpsertPoiRequestDto request, CancellationToken cancellationToken = default)
    {
        var poi = await _dbContext.Pois
            .Include(x => x.Localizations)
            .Include(x => x.AudioAssets)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (poi is null)
        {
            return false;
        }

        ApplyRequest(poi, request);
        poi.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var poi = await _dbContext.Pois.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (poi is null)
        {
            return false;
        }

        _dbContext.Pois.Remove(poi);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static PoiMobileDto MapToMobileDto(Poi poi, string? requestedLanguageCode, IReadOnlyDictionary<int, double>? distanceByPoiId = null)
    {
        var requestedLanguage = NormalizeLanguageCode(requestedLanguageCode);
        var primaryLanguage = NormalizeLanguageCode(poi.PrimaryLanguage);

        var localization = ResolveLocalization(poi, requestedLanguage, primaryLanguage);
        var effectiveLanguage = localization?.LanguageCode ?? primaryLanguage;

        var dto = new PoiMobileDto
        {
            Id = poi.Id,
            Title = localization?.Title ?? poi.Title,
            Subtitle = localization?.Subtitle ?? poi.Subtitle ?? string.Empty,
            Description = localization?.Description ?? poi.Description ?? string.Empty,
            LanguageCode = effectiveLanguage,
            PrimaryLanguage = primaryLanguage,
            ImageUrl = poi.ImageUrl ?? string.Empty,
            Location = poi.Location ?? string.Empty,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            DistanceMeters = distanceByPoiId is not null && distanceByPoiId.TryGetValue(poi.Id, out var distance) ? distance : null,
            GeofenceRadiusMeters = poi.GeofenceRadiusMeters,
            Category = poi.Category ?? string.Empty,
            SpeechText = poi.SpeechText,
            AudioAssets = poi.AudioAssets
                .OrderByDescending(x => string.Equals(x.LanguageCode, requestedLanguage, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(x => string.Equals(x.LanguageCode, primaryLanguage, StringComparison.OrdinalIgnoreCase))
                .Select(x => new PoiAudioMobileDto
                {
                    Id = x.Id,
                    LanguageCode = x.LanguageCode,
                    AudioUrl = x.AudioUrl,
                    Transcript = x.Transcript,
                    IsGenerated = x.IsGenerated
                })
                .ToList()
        };

        return dto;
    }

    private static bool HasGeoFilter(PoiQueryRequestDto request)
    {
        return request.Latitude.HasValue
               && request.Longitude.HasValue
               && request.RadiusMeters.HasValue
               && request.RadiusMeters.Value > 0;
    }

    private static double CalculateHaversineDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double ToRadians(double value)
    {
        return value * Math.PI / 180d;
    }

    private static PoiLocalization? ResolveLocalization(Poi poi, string requestedLanguage, string primaryLanguage)
    {
        return poi.Localizations.FirstOrDefault(x => string.Equals(x.LanguageCode, requestedLanguage, StringComparison.OrdinalIgnoreCase))
               ?? poi.Localizations.FirstOrDefault(x => string.Equals(x.LanguageCode, primaryLanguage, StringComparison.OrdinalIgnoreCase))
               ?? poi.Localizations.FirstOrDefault(x => string.Equals(x.LanguageCode, "en", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? "en"
            : languageCode.Trim().ToLowerInvariant();
    }

    private static void ApplyRequest(Poi poi, UpsertPoiRequestDto request)
    {
        poi.Title = request.Title;
        poi.Subtitle = request.Subtitle;
        poi.Description = request.Description;
        poi.Category = request.Category;
        poi.Location = request.Location;
        poi.ImageUrl = request.ImageUrl;
        poi.Latitude = request.Latitude;
        poi.Longitude = request.Longitude;
        poi.GeofenceRadiusMeters = request.GeofenceRadiusMeters;
        poi.PrimaryLanguage = NormalizeLanguageCode(request.PrimaryLanguage);
        poi.SpeechText = request.SpeechText?.Trim();

        poi.Localizations.Clear();
        foreach (var localization in request.Localizations)
        {
            poi.Localizations.Add(new PoiLocalization
            {
                LanguageCode = NormalizeLanguageCode(localization.LanguageCode),
                Title = localization.Title,
                Subtitle = localization.Subtitle,
                Description = localization.Description
            });
        }

        poi.AudioAssets.Clear();
        foreach (var audio in request.AudioAssets)
        {
            poi.AudioAssets.Add(new PoiAudio
            {
                LanguageCode = NormalizeLanguageCode(audio.LanguageCode),
                AudioUrl = audio.AudioUrl,
                Transcript = audio.Transcript,
                IsGenerated = audio.IsGenerated,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
    }
}

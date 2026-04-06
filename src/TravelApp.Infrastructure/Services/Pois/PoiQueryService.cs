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
    private readonly TravelApp.Application.Abstractions.ITranslationService _translationService;

    public PoiQueryService(TravelAppDbContext dbContext, TravelApp.Application.Abstractions.ITranslationService translationService)
    {
        _dbContext = dbContext;
        _translationService = translationService;
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
        var requestedLanguage = NormalizeLanguageCode(languageCode);

        // Load poi with related collections
        var poi = await _dbContext.Pois
            .Include(x => x.Localizations)
            .Include(x => x.AudioAssets)
            .Include(x => x.Stories)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (poi is null)
            return null;

        // If requested language not present in localizations or stories, attempt server-side translation
        var hasLocalization = poi.Localizations.Any(x => string.Equals(x.LanguageCode, requestedLanguage, StringComparison.OrdinalIgnoreCase));

        // Load stories if any (optional relationship)
        var hasStories = false;
        try
        {
            var stories = poi.Stories?.ToList() ?? new List<Domain.Entities.PoiStory>();
            hasStories = stories.Any(s => string.Equals(s.LanguageCode, requestedLanguage, StringComparison.OrdinalIgnoreCase));

            if (!hasLocalization || !hasStories)
            {
                // perform translation where needed
                // translate title/subtitle/description if missing
                if (!hasLocalization)
                {
                    var title = await _translationService.TranslateTextAsync(poi.Title, requestedLanguage, cancellationToken) ?? poi.Title;
                    var subtitle = await _translationService.TranslateTextAsync(poi.Subtitle ?? string.Empty, requestedLanguage, cancellationToken) ?? poi.Subtitle;
                    var description = await _translationService.TranslateTextAsync(poi.Description ?? string.Empty, requestedLanguage, cancellationToken) ?? poi.Description;

                    var loc = new PoiLocalization
                    {
                        PoiId = poi.Id,
                        LanguageCode = requestedLanguage,
                        Title = title,
                        Subtitle = subtitle,
                        Description = description
                    };
                    _dbContext.PoiLocalizations.Add(loc);
                }

                if (!hasStories)
                {
                    var existingStories = stories;
                    var storiesToTranslate = existingStories.Where(s => string.Equals(s.LanguageCode, poi.PrimaryLanguage, StringComparison.OrdinalIgnoreCase)).ToList();
                    foreach (var s in storiesToTranslate)
                    {
                        var tTitle = await _translationService.TranslateTextAsync(s.Title, requestedLanguage, cancellationToken) ?? s.Title;
                        var tContent = await _translationService.TranslateTextAsync(s.Content, requestedLanguage, cancellationToken) ?? s.Content;
                        var newStory = new Domain.Entities.PoiStory
                        {
                            PoiId = poi.Id,
                            LanguageCode = requestedLanguage,
                            Title = tTitle,
                            Content = tContent,
                            OrderIndex = s.OrderIndex
                        };
                        _dbContext.Set<Domain.Entities.PoiStory>().Add(newStory);
                    }
                }

                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch
                {
                    // If save fails, swallow to allow fallback to primary language
                }
            }
        }
        catch (Exception)
        {
            // On any translation failure, continue and return existing data (fallback)
        }

        // reload poi as no-tracking for mapping
        var poiReload = await _dbContext.Pois
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Include(x => x.Localizations)
            .Include(x => x.AudioAssets)
            .FirstOrDefaultAsync(cancellationToken);

        return MapToMobileDto(poiReload!, languageCode);
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
            ,
            Stories = MapStories(poi, requestedLanguage, primaryLanguage)
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

    private static List<PoiStoryDto> MapStories(Poi poi, string requestedLanguage, string primaryLanguage)
    {
        var stories = poi.Stories ?? new List<PoiStory>();

        // Prefer stories in requested language, then primary, then any
        var selected = stories.Where(s => string.Equals(s.LanguageCode, requestedLanguage, StringComparison.OrdinalIgnoreCase)).ToList();
        if (selected.Count == 0)
            selected = stories.Where(s => string.Equals(s.LanguageCode, primaryLanguage, StringComparison.OrdinalIgnoreCase)).ToList();
        if (selected.Count == 0)
            selected = stories.ToList();

        return selected.OrderBy(s => s.OrderIndex).Select(s => new PoiStoryDto
        {
            Id = s.Id,
            LanguageCode = s.LanguageCode,
            Title = s.Title,
            Content = s.Content,
            OrderIndex = s.OrderIndex
        }).ToList();
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

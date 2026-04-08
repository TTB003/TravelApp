using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using TravelApp.Application.Abstractions.Tours;
using TravelApp.Application.Dtos.Pois;
using TravelApp.Application.Dtos.Tours;
using TravelApp.Infrastructure.Persistence;

namespace TravelApp.Infrastructure.Services.Tours;

public class TourQueryService : ITourQueryService
{
    private readonly TravelAppDbContext _dbContext;

    public TourQueryService(TravelAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TourRouteDto?> GetByAnchorPoiIdAsync(int anchorPoiId, string? languageCode, CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = NormalizeLanguageCode(languageCode);

        TravelApp.Domain.Entities.Tour? tour;

        try
        {
            tour = await _dbContext.Tours
                .AsNoTracking()
                .Include(x => x.TourPois)
                    .ThenInclude(x => x.Poi)
                        .ThenInclude(x => x.Localizations)
                .Include(x => x.TourPois)
                    .ThenInclude(x => x.Poi)
                        .ThenInclude(x => x.AudioAssets)
                .FirstOrDefaultAsync(x => x.AnchorPoiId == anchorPoiId && x.IsPublished, cancellationToken);
        }
        catch (SqlException ex) when (ex.Number == 208)
        {
            return null;
        }

        if (tour is null)
        {
            return null;
        }

        var waypoints = tour.TourPois
            .OrderBy(x => x.SortOrder)
            .Select(x => new TourRouteWaypointDto
            {
                SortOrder = x.SortOrder,
                DistanceFromPreviousMeters = x.DistanceFromPreviousMeters,
                Poi = MapPoi(x.Poi, normalizedLanguage)
            })
            .ToList();

        return new TourRouteDto
        {
            Id = tour.Id,
            AnchorPoiId = tour.AnchorPoiId,
            Name = tour.Name,
            Description = tour.Description,
            CoverImageUrl = NormalizeCoverImageUrl(tour.CoverImageUrl, tour.Name),
            PrimaryLanguage = NormalizeLanguageCode(tour.PrimaryLanguage),
            TotalDistanceMeters = waypoints.Sum(x => x.DistanceFromPreviousMeters ?? 0),
            Waypoints = waypoints
        };
    }

    private static PoiMobileDto MapPoi(TravelApp.Domain.Entities.Poi poi, string languageCode)
    {
        var primaryLanguage = NormalizeLanguageCode(poi.PrimaryLanguage);
        var localization = poi.Localizations.FirstOrDefault(x => string.Equals(x.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
            ?? poi.Localizations.FirstOrDefault(x => string.Equals(x.LanguageCode, primaryLanguage, StringComparison.OrdinalIgnoreCase))
            ?? poi.Localizations.FirstOrDefault(x => string.Equals(x.LanguageCode, "en", StringComparison.OrdinalIgnoreCase));

        return new PoiMobileDto
        {
            Id = poi.Id,
            Title = localization?.Title ?? poi.Title,
            Subtitle = localization?.Subtitle ?? poi.Subtitle ?? string.Empty,
            Description = localization?.Description ?? poi.Description ?? string.Empty,
            LanguageCode = localization?.LanguageCode ?? primaryLanguage,
            PrimaryLanguage = primaryLanguage,
            ImageUrl = poi.ImageUrl ?? string.Empty,
            Location = poi.Location ?? string.Empty,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            DistanceMeters = null,
            GeofenceRadiusMeters = poi.GeofenceRadiusMeters,
            Category = poi.Category ?? string.Empty,
            SpeechText = poi.SpeechText,
            AudioAssets = poi.AudioAssets
                .OrderByDescending(x => string.Equals(x.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
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
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim().ToLowerInvariant();
    }

    private static string NormalizeCoverImageUrl(string? coverImageUrl, string tourName)
    {
        if (!string.IsNullOrWhiteSpace(coverImageUrl)
            && !coverImageUrl.Contains("unsplash.com", StringComparison.OrdinalIgnoreCase))
        {
            return coverImageUrl;
        }

        return $"https://placehold.co/1200x800/png?text={Uri.EscapeDataString(tourName)}";
    }
}

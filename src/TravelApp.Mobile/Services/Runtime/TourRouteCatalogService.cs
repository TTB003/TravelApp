using TravelApp.Models.Contracts;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public sealed class TourRouteCatalogService : ITourRouteCatalogService
{
    private readonly ITourApiClient _tourApiClient;
    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly ITourRouteCacheService _tourRouteCacheService;

    public TourRouteCatalogService(
        ITourApiClient tourApiClient,
        ILocalDatabaseService localDatabaseService,
        ITourRouteCacheService tourRouteCacheService)
    {
        _tourApiClient = tourApiClient;
        _localDatabaseService = localDatabaseService;
        _tourRouteCacheService = tourRouteCacheService;
    }

    public async Task<TourRouteDto?> GetRouteAsync(int anchorPoiId, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = NormalizeLanguageCode(languageCode);
        TourRouteDto? route = null;

        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            try
            {
                route = await _tourApiClient.GetByAnchorPoiIdAsync(anchorPoiId, normalizedLanguage, cancellationToken);
                if (route is not null && IsAcceptableRoute(route))
                {
                    route = await MergeLocalPoiOverridesAsync(route, normalizedLanguage, cancellationToken);
                    await _localDatabaseService.SavePoisAsync(route.Waypoints.Select(x => x.Poi), cancellationToken);
                    await _tourRouteCacheService.SaveAsync(route, cancellationToken);
                    return route;
                }
            }
            catch
            {
            }
        }

        route = await _tourRouteCacheService.GetAsync(anchorPoiId, normalizedLanguage, cancellationToken);
        var cached = route;
        if (cached is not null && cached.Waypoints.Count > 0)
        {
            return await MergeLocalPoiOverridesAsync(cached, normalizedLanguage, cancellationToken);
        }

        route = BuildLocalFallbackRoute(anchorPoiId, normalizedLanguage);
        return route is null ? null : await MergeLocalPoiOverridesAsync(route, normalizedLanguage, cancellationToken);
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim().ToLowerInvariant();
    }

    private static bool IsAcceptableRoute(TourRouteDto? route)
    {
        if (route is null || route.Waypoints.Count < 2)
        {
            return false;
        }

        return !route.Waypoints.Any(x =>
            x.Poi.Title.Contains("Central Park", StringComparison.OrdinalIgnoreCase) ||
            x.Poi.Location.Contains("New York", StringComparison.OrdinalIgnoreCase) ||
            x.Poi.Location.Contains("USA", StringComparison.OrdinalIgnoreCase));
    }

    private static TourRouteDto? BuildLocalFallbackRoute(int anchorPoiId, string languageCode)
    {
        return anchorPoiId switch
        {
            1 or 2 or 3 => new TourRouteDto
            {
                Id = 1,
                AnchorPoiId = 1,
                Name = "HCM Food Tour",
                Description = "Tour ẩm thực Sài Gòn với các điểm dừng được sắp xếp theo lộ trình thật.",
                CoverImageUrl = "https://placehold.co/1200x800/png?text=HCM+Food+Tour",
                PrimaryLanguage = languageCode,
                TotalDistanceMeters = 2000,
                Waypoints =
                [
                    CreateWaypoint(1, 1, "Chợ Bến Thành", "Chợ Bến Thành, Quận 1, TPHCM", 10.7725, 106.6992, 0, "Điểm khởi đầu của tour ẩm thực HCM. Chợ Bến Thành là một trong những chợ truyền thống nổi tiếng nhất Sài Gòn với đa dạng hàng hóa và đặc biệt là các quán ăn địa phương."),
                    CreateWaypoint(2, 2, "Phở Vĩnh Khánh", "Phố Vĩnh Khánh, Quận 4, TPHCM", 10.7660, 106.7090, 900, "Quán phở nổi tiếng với nước dùng được ninh từ 12h, phục vụ phở bò ngon nhất Quận 4. Được nhiều du khách lựa chọn trong tour ẩm thực."),
                    CreateWaypoint(3, 3, "Bến Bạch Đằng", "Bến Bạch Đằng, Quận 1, TPHCM", 10.7558, 106.7062, 1100, "Kết thúc tour tại bến Bạch Đằng. Thưởng thức các đặc sản Sài Gòn và tận hưởng không khí ven sông.")
                ]
            },
            4 or 5 or 6 => new TourRouteDto
            {
                Id = 2,
                AnchorPoiId = 4,
                Name = "Hanoi Food Tour",
                Description = "Tour ẩm thực Hà Nội với các mốc waypoint, bản đồ và audio tự động.",
                CoverImageUrl = "https://placehold.co/1200x800/png?text=Hanoi+Food+Tour",
                PrimaryLanguage = languageCode,
                TotalDistanceMeters = 800,
                Waypoints =
                [
                    CreateWaypoint(4, 4, "Chùa Một Cột", "Chùa Một Cột, Quận Ba Đình, Hà Nội", 21.0294, 105.8352, 0, "Điểm khởi đầu của tour ẩm thực Hà Nội. Chùa Một Cột là một di tích lịch sử quan trọng, nằm gần khu phố cổ Hà Nội."),
                    CreateWaypoint(5, 5, "Phố Hàng Xanh", "Phố Hàng Xanh, Quận Hoàn Kiếm, Hà Nội", 21.0285, 105.8489, 300, "Phố Hàng Xanh là một trong những phố cổ nổi tiếng của Hà Nội với các quán ăn truyền thống.") ,
                    CreateWaypoint(6, 6, "Phố Hàng Dâu", "Phố Hàng Dâu, Quận Hoàn Kiếm, Hà Nội", 21.0273, 105.8506, 500, "Kết thúc tour tại phố Hàng Dâu. Nơi đây nổi tiếng với các cửa hàng bán lụa truyền thống và các quán ăn địa phương.")
                ]
            },
            _ => null
        };
    }

    private static TourRouteWaypointDto CreateWaypoint(int poiId, int sortOrder, string title, string location, double latitude, double longitude, double distanceMeters, string description)
    {
        return new TourRouteWaypointDto
        {
            SortOrder = sortOrder,
            DistanceFromPreviousMeters = distanceMeters,
            Poi = new PoiMobileDto
            {
                Id = poiId,
                Title = title,
                Subtitle = description,
                Description = description,
                SpeechText = description,
                LanguageCode = "vi",
                PrimaryLanguage = "vi",
                ImageUrl = string.Empty,
                Location = location,
                Latitude = latitude,
                Longitude = longitude,
                GeofenceRadiusMeters = 150,
                Category = "Food Tour",
            }
        };
    }

    private async Task<TourRouteDto> MergeLocalPoiOverridesAsync(TourRouteDto route, string languageCode, CancellationToken cancellationToken)
    {
        var localPois = await _localDatabaseService.GetPoisAsync(languageCode, cancellationToken: cancellationToken);
        var localById = localPois.ToDictionary(x => x.Id);

        route.Waypoints = route.Waypoints
            .Select(waypoint =>
            {
                if (!localById.TryGetValue(waypoint.Poi.Id, out var localPoi))
                {
                    return waypoint;
                }

                waypoint.Poi = localPoi;
                return waypoint;
            })
            .ToList();

        route.CoverImageUrl = NormalizeCoverImageUrl(route.CoverImageUrl, route.Name);

        return route;
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

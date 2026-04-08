using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Devices.Sensors;
using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public sealed class AzureMapsRouteGeometryService : ITourRouteGeometryService
{
    private static string RouteGeometryCacheVersion => "v4";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AzureMapsRouteGeometryService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<RouteGeometryResult> GetRoadPathAsync(TourRouteDto route, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        if (route.Waypoints.Count < 2)
        {
            return BuildFallbackGeometry(route);
        }

        var normalizedLanguage = Normalize(languageCode ?? route.PrimaryLanguage);
        var cachePath = GetCachePath(route.AnchorPoiId, normalizedLanguage);

        if (File.Exists(cachePath))
        {
            var cached = await TryReadCacheAsync(cachePath, cancellationToken);
            if (cached.Segments.Count > 0)
            {
                return cached;
            }
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            return BuildFallbackGeometry(route);
        }

        var subscriptionKey = ResolveSubscriptionKey();
        if (string.IsNullOrWhiteSpace(subscriptionKey))
        {
            return BuildFallbackGeometry(route);
        }

        try
        {
            var geometry = await RequestRoadGeometryAsync(route, subscriptionKey, cancellationToken);
            if (geometry.Segments.Count == 0)
            {
                return BuildFallbackGeometry(route);
            }

            await SaveCacheAsync(cachePath, geometry, cancellationToken);
            return geometry;
        }
        catch
        {
            return BuildFallbackGeometry(route);
        }
    }

    private static RouteGeometryResult BuildFallbackGeometry(TourRouteDto route)
    {
        var segments = new List<RouteGeometrySegment>();
        for (var i = 0; i < route.Waypoints.Count - 1; i++)
        {
            var start = route.Waypoints[i].Poi;
            var end = route.Waypoints[i + 1].Poi;
            segments.Add(new RouteGeometrySegment
            {
                Index = i,
                Label = $"Stop {i + 1}",
                Points =
                [
                    new Location(start.Latitude, start.Longitude),
                    new Location(end.Latitude, end.Longitude)
                ]
            });
        }

        return new RouteGeometryResult { Segments = segments };
    }

    private async Task<RouteGeometryResult> RequestRoadGeometryAsync(TourRouteDto route, string subscriptionKey, CancellationToken cancellationToken)
    {
        var query = string.Join(":", route.Waypoints.Select(x => FormattableString.Invariant($"{x.Poi.Latitude},{x.Poi.Longitude}")));
        var url = $"https://atlas.microsoft.com/route/directions/json?api-version=1.0&subscription-key={Uri.EscapeDataString(subscriptionKey)}&query={Uri.EscapeDataString(query)}&travelMode=driving&traffic=true&routeOutputOptions=routePath";

        var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new RouteGeometryResult();
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return ParseRouteGeometry(document.RootElement, route);
    }

    private static RouteGeometryResult ParseRouteGeometry(JsonElement root, TourRouteDto route)
    {
        var segments = new List<RouteGeometrySegment>();

        if (root.TryGetProperty("routes", out var routes) && routes.ValueKind == JsonValueKind.Array && routes.GetArrayLength() > 0)
        {
            var routeNode = routes[0];
            if (routeNode.TryGetProperty("legs", out var legs) && legs.ValueKind == JsonValueKind.Array)
            {
                var legIndex = 0;
                foreach (var leg in legs.EnumerateArray())
                {
                    if (!leg.TryGetProperty("points", out var legPoints) || legPoints.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    var points = new List<Location>();
                    foreach (var point in legPoints.EnumerateArray())
                    {
                        AddPoint(points, point);
                    }

                    points = Deduplicate(points).ToList();
                    if (points.Count > 0)
                    {
                        segments.Add(new RouteGeometrySegment
                        {
                            Index = legIndex,
                            Label = GetSegmentLabel(route, legIndex),
                            Points = points
                        });
                    }

                    legIndex++;
                }
            }
        }

        if (segments.Count > 0)
        {
            return new RouteGeometryResult { Segments = segments };
        }

        if (root.TryGetProperty("features", out var features) && features.ValueKind == JsonValueKind.Array)
        {
            var roadPoints = new List<Location>();
            foreach (var feature in features.EnumerateArray())
            {
                if (!feature.TryGetProperty("geometry", out var geometry))
                {
                    continue;
                }

                var type = geometry.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
                if (string.Equals(type, "LineString", StringComparison.OrdinalIgnoreCase))
                {
                    if (geometry.TryGetProperty("coordinates", out var coordinates))
                    {
                        ExtractCoordinates(roadPoints, coordinates);
                    }
                }
                else if (string.Equals(type, "MultiLineString", StringComparison.OrdinalIgnoreCase) && geometry.TryGetProperty("coordinates", out var multiCoordinates))
                {
                    foreach (var segment in multiCoordinates.EnumerateArray())
                    {
                        ExtractCoordinates(roadPoints, segment);
                    }
                }
            }

            roadPoints = Deduplicate(roadPoints).ToList();
            if (roadPoints.Count > 0)
            {
                return new RouteGeometryResult
                {
                    Segments =
                    [
                        new RouteGeometrySegment
                        {
                            Index = 0,
                            Label = "Route",
                            Points = roadPoints
                        }
                    ]
                };
            }
        }

        return new RouteGeometryResult();
    }

    private static string GetSegmentLabel(TourRouteDto route, int legIndex)
    {
        if (legIndex >= 0 && legIndex < route.Waypoints.Count - 1)
        {
            return $"{route.Waypoints[legIndex].Poi.Title} → {route.Waypoints[legIndex + 1].Poi.Title}";
        }

        return $"Segment {legIndex + 1}";
    }

    private static void ExtractCoordinates(ICollection<Location> points, JsonElement coordinates)
    {
        if (coordinates.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var coordinate in coordinates.EnumerateArray())
        {
            AddPoint(points, coordinate);
        }
    }

    private static void AddPoint(ICollection<Location> points, JsonElement point)
    {
        if (point.ValueKind == JsonValueKind.Array && point.GetArrayLength() >= 2)
        {
            var lon = point[0].GetDouble();
            var lat = point[1].GetDouble();
            points.Add(new Location(lat, lon));
            return;
        }

        if (point.ValueKind == JsonValueKind.Object)
        {
            if (point.TryGetProperty("longitude", out var longitude) && point.TryGetProperty("latitude", out var latitude))
            {
                points.Add(new Location(latitude.GetDouble(), longitude.GetDouble()));
                return;
            }

            if (point.TryGetProperty("coordinates", out var coordinates) && coordinates.ValueKind == JsonValueKind.Array && coordinates.GetArrayLength() >= 2)
            {
                var lon = coordinates[0].GetDouble();
                var lat = coordinates[1].GetDouble();
                points.Add(new Location(lat, lon));
            }
        }
    }

    private static IEnumerable<Location> Deduplicate(IReadOnlyList<Location> points)
    {
        Location? previous = null;
        foreach (var point in points)
        {
            if (previous is not null && Math.Abs(previous.Latitude - point.Latitude) < 0.000001 && Math.Abs(previous.Longitude - point.Longitude) < 0.000001)
            {
                continue;
            }

            yield return point;
            previous = point;
        }
    }

    private static async Task SaveCacheAsync(string cachePath, RouteGeometryResult geometry, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(cachePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var payload = new RouteCachePayload
        {
            Segments = geometry.Segments.Select(segment => new RouteCacheSegment
            {
                Index = segment.Index,
                Label = segment.Label,
                Points = segment.Points.Select(x => new RouteCachePoint(x.Latitude, x.Longitude)).ToList()
            }).ToList()
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await File.WriteAllTextAsync(cachePath, json, cancellationToken);
    }

    private static async Task<RouteGeometryResult> TryReadCacheAsync(string cachePath, CancellationToken cancellationToken)
    {
        try
        {
            var json = await File.ReadAllTextAsync(cachePath, cancellationToken);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                var points = root.EnumerateArray()
                    .Select(x => new Location(x.GetProperty("Latitude").GetDouble(), x.GetProperty("Longitude").GetDouble()))
                    .ToList();

                return new RouteGeometryResult
                {
                    Segments =
                    [
                        new RouteGeometrySegment
                        {
                            Index = 0,
                            Label = "Route",
                            Points = points
                        }
                    ]
                };
            }

            var payload = JsonSerializer.Deserialize<RouteCachePayload>(json, JsonOptions);
            if (payload?.Segments is null || payload.Segments.Count == 0)
            {
                return new RouteGeometryResult();
            }

            return new RouteGeometryResult
            {
                Segments = payload.Segments
                    .OrderBy(x => x.Index)
                    .Select(segment => new RouteGeometrySegment
                    {
                        Index = segment.Index,
                        Label = segment.Label,
                        Points = segment.Points.Select(x => new Location(x.Latitude, x.Longitude)).ToList()
                    })
                    .ToList()
            };
        }
        catch
        {
            return new RouteGeometryResult();
        }
    }

    private static string ResolveSubscriptionKey()
    {
        return Environment.GetEnvironmentVariable("AZURE_MAPS_KEY")
            ?? Environment.GetEnvironmentVariable("AZURE_MAPS_SUBSCRIPTION_KEY")
            ?? Environment.GetEnvironmentVariable("BING_MAPS_KEY")
            ?? string.Empty;
    }

    private static string Normalize(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim().ToLowerInvariant();
    }

    private static string GetCachePath(int anchorPoiId, string languageCode)
    {
        var cacheDirectory = Path.Combine(FileSystem.AppDataDirectory, "tour-route-geometry-cache");
        return Path.Combine(cacheDirectory, $"{RouteGeometryCacheVersion}-tour-{anchorPoiId}-{Normalize(languageCode)}.json");
    }

    private sealed class RouteCachePayload
    {
        public List<RouteCacheSegment> Segments { get; set; } = [];
    }

    private sealed class RouteCacheSegment
    {
        public int Index { get; set; }
        public string Label { get; set; } = string.Empty;
        public List<RouteCachePoint> Points { get; set; } = [];
    }

    private sealed record RouteCachePoint(double Latitude, double Longitude);
}

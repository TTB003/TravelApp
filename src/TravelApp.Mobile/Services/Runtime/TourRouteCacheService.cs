using System.Text.Json;
using TravelApp.Models.Contracts;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public sealed class TourRouteCacheService : ITourRouteCacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<TourRouteDto?> GetAsync(int anchorPoiId, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var path = GetCachePath(anchorPoiId, languageCode);
        if (!File.Exists(path))
        {
            return null;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken);
            return JsonSerializer.Deserialize<TourRouteDto>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(TourRouteDto route, CancellationToken cancellationToken = default)
    {
        var path = GetCachePath(route.AnchorPoiId, route.PrimaryLanguage);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(route, JsonOptions);
            await File.WriteAllTextAsync(path, json, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task InvalidateAsync(int anchorPoiId, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var cacheDirectory = Path.Combine(FileSystem.AppDataDirectory, "tour-route-cache");
        var language = string.IsNullOrWhiteSpace(languageCode) ? null : languageCode.Trim().ToLowerInvariant();

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (language is not null)
            {
                var path = GetCachePath(anchorPoiId, language);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                return;
            }

            if (!Directory.Exists(cacheDirectory))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(cacheDirectory, $"tour-{anchorPoiId}-*.json"))
            {
                File.Delete(file);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string GetCachePath(int anchorPoiId, string? languageCode)
    {
        var language = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim().ToLowerInvariant();
        var cacheDirectory = Path.Combine(FileSystem.AppDataDirectory, "tour-route-cache");
        return Path.Combine(cacheDirectory, $"tour-{anchorPoiId}-{language}.json");
    }
}

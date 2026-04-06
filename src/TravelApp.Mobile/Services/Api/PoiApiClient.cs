using TravelApp.Models.Contracts;
using TravelApp.Services.Abstractions;
using System.Net.Http.Json;

namespace TravelApp.Services.Api;

public class PoiApiClient : ApiClientBase, IPoiApiClient
{
    public PoiApiClient(IHttpClientFactory httpClientFactory, ApiClientOptions options, ITokenStore tokenStore)
        : base(httpClientFactory, options, tokenStore)
    {
    }

    private static PoiDto MapToPoiDto(PoiMobileDto src)
    {
        return new PoiDto
        {
            Id = src.Id,
            Title = src.Title,
            Subtitle = src.Subtitle,
            Description = src.Description,
            // API returns language information on the item level via PrimaryLanguage / LanguageCode in payload;
            // mobile runtime expects PrimaryLanguage on the POI and localizations collection for translations.
            PrimaryLanguage = src.PrimaryLanguage,
            ImageUrl = src.ImageUrl,
            Location = src.Location,
            Latitude = src.Latitude,
            Longitude = src.Longitude,
            GeofenceRadiusMeters = src.GeofenceRadiusMeters,
            Category = src.Category,
            AudioAssets = src.AudioAssets?.Select(a => new PoiAudioDto(a.LanguageCode, a.AudioUrl, a.Transcript, a.IsGenerated)).ToList() ?? new List<PoiAudioDto>(),
            Localizations = new List<PoiLocalizationDto>(),
            Stories = src.Stories?.Select(s => new Story
            {
                Title = s.Title,
                Content = s.Content,
                LanguageCode = s.LanguageCode
            }).ToList() ?? new List<Story>()
        };
    }

    public async Task<IReadOnlyList<PoiDto>> GetAllAsync(string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var endpoint = string.IsNullOrWhiteSpace(languageCode)
            ? "api/pois"
            : $"api/pois?lang={Uri.EscapeDataString(languageCode)}";
        var response = await client.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new List<PoiDto>();

        var payload = await response.Content.ReadFromJsonAsync<PagedResultDto<PoiMobileDto>>(JsonOptions, cancellationToken);
        var items = payload?.Items ?? new List<PoiMobileDto>();
        return items.Select(MapToPoiDto).ToList();
    }

    public async Task<IReadOnlyList<PoiDto>> GetNearbyAsync(NearbyPoiQueryDto query, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var queryString =
            $"lat={query.Latitude}&lng={query.Longitude}&radiusMeters={query.RadiusMeters}";

        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            queryString += $"&lang={Uri.EscapeDataString(languageCode)}";
        }

        // The API exposes nearby filtering via the same GET /api/pois endpoint with lat/lng/radius
        var response = await client.GetAsync($"api/pois?{queryString}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new List<PoiDto>();

        var payload = await response.Content.ReadFromJsonAsync<PagedResultDto<PoiMobileDto>>(JsonOptions, cancellationToken);
        var items = payload?.Items ?? new List<PoiMobileDto>();
        return items.Select(MapToPoiDto).ToList();
    }

    public async Task<PoiDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var response = await client.GetAsync($"api/pois/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var item = await response.Content.ReadFromJsonAsync<PoiMobileDto>(JsonOptions, cancellationToken);
        return item is null ? null : MapToPoiDto(item);
    }

    public async Task<PoiDto?> GetByIdAsync(int id, string? languageCode, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var endpoint = string.IsNullOrWhiteSpace(languageCode) ? $"api/pois/{id}" : $"api/pois/{id}?lang={Uri.EscapeDataString(languageCode)}";
        var response = await client.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var item = await response.Content.ReadFromJsonAsync<PoiMobileDto>(JsonOptions, cancellationToken);
        return item is null ? null : MapToPoiDto(item);
    }

    public async Task<PoiDto?> CreateAsync(UpsertPoiRequestDto request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(authorized: true);
        var response = await client.PostAsJsonAsync("api/pois", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var created = await response.Content.ReadFromJsonAsync<PoiMobileDto>(JsonOptions, cancellationToken);
        return created is null ? null : MapToPoiDto(created);
    }

    public async Task<bool> UpdateAsync(int id, UpsertPoiRequestDto request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(authorized: true);
        var response = await client.PutAsJsonAsync($"api/pois/{id}", request, JsonOptions, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(authorized: true);
        var response = await client.DeleteAsync($"api/pois/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}

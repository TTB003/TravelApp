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

    public async Task<IReadOnlyList<PoiDto>> GetAllAsync(string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var endpoint = string.IsNullOrWhiteSpace(languageCode)
            ? "api/pois"
            : $"api/pois?lang={Uri.EscapeDataString(languageCode)}";

        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, endpoint), cancellationToken: cancellationToken);
        return await ReadAsAsync<List<PoiDto>>(response, cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<PoiDto>> GetNearbyAsync(NearbyPoiQueryDto query, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var queryString =
            $"lat={query.Latitude}&lng={query.Longitude}&radiusMeters={query.RadiusMeters}";

        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            queryString += $"&lang={Uri.EscapeDataString(languageCode)}";
        }

        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, $"api/pois?{queryString}"), cancellationToken: cancellationToken);
        return await ReadAsAsync<List<PoiDto>>(response, cancellationToken) ?? [];
    }

    public async Task<PoiDto?> GetByIdAsync(int id, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var endpoint = string.IsNullOrWhiteSpace(languageCode)
            ? $"api/pois/{id}"
            : $"api/pois/{id}?lang={Uri.EscapeDataString(languageCode)}";

        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, endpoint), cancellationToken: cancellationToken);
        return await ReadAsAsync<PoiDto>(response, cancellationToken);
    }

    public async Task<PoiDto?> CreateAsync(UpsertPoiRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, "api/pois")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        }, authorized: true, cancellationToken);
        return await ReadAsAsync<PoiDto>(response, cancellationToken);
    }

    public async Task<bool> UpdateAsync(int id, UpsertPoiRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Put, $"api/pois/{id}")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        }, authorized: true, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, $"api/pois/{id}"), authorized: true, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task TrackAudioPlayAsync(int id)
    {
        await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, $"api/pois/{id}/audio-play"), authorized: true);
    }
}

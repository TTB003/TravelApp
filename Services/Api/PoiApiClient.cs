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
        var client = CreateClient();
        var endpoint = string.IsNullOrWhiteSpace(languageCode)
            ? "poi/load-all"
            : $"poi/load-all?lang={Uri.EscapeDataString(languageCode)}";

        var response = await client.GetAsync(endpoint, cancellationToken);
        return await ReadAsAsync<List<PoiDto>>(response, cancellationToken) ?? [];
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

        var response = await client.GetAsync($"poi/nearby?{queryString}", cancellationToken);
        return await ReadAsAsync<List<PoiDto>>(response, cancellationToken) ?? [];
    }

    public async Task<PoiDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var response = await client.GetAsync($"poi/{id}", cancellationToken);
        return await ReadAsAsync<PoiDto>(response, cancellationToken);
    }

    public async Task<PoiDto?> CreateAsync(UpsertPoiRequestDto request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(authorized: true);
        var response = await client.PostAsJsonAsync("poi", request, JsonOptions, cancellationToken);
        return await ReadAsAsync<PoiDto>(response, cancellationToken);
    }

    public async Task<bool> UpdateAsync(int id, UpsertPoiRequestDto request, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(authorized: true);
        var response = await client.PutAsJsonAsync($"poi/{id}", request, JsonOptions, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(authorized: true);
        var response = await client.DeleteAsync($"poi/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}

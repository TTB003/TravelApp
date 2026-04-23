using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TravelApp.Application.Dtos.Pois;
using TravelApp.Application.Dtos.Users;
using TravelApp.Application.Dtos.Tours;
using TravelApp.Admin.Web.Models;

namespace TravelApp.Admin.Web.Services;

public sealed class TravelAppApiClient : ITravelAppApiClient
{
    private readonly HttpClient _httpClient;
    private readonly Microsoft.Extensions.Logging.ILogger<TravelAppApiClient> _logger;

    public TravelAppApiClient(HttpClient httpClient, IOptions<TravelAppApiOptions> options, Microsoft.Extensions.Logging.ILogger<TravelAppApiClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl);
        _logger = logger;
    }

    public async Task<IReadOnlyList<object>> GetTopPoisAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = $"api/metrics/admin/top-pois?limit={limit}";
            return await _httpClient.GetFromJsonAsync<List<object>>(endpoint, cancellationToken) ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<IReadOnlyList<PoiMobileDto>> GetPoisAsync(string? languageCode = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var items = new List<PoiMobileDto>();
            var pageNumber = 1;
            const int pageSize = 100;

            while (true)
            {
                var endpoint = string.IsNullOrWhiteSpace(languageCode)
                    ? $"api/pois?pageNumber={pageNumber}&pageSize={pageSize}"
                    : $"api/pois?lang={Uri.EscapeDataString(languageCode)}&pageNumber={pageNumber}&pageSize={pageSize}";

                var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                response.EnsureSuccessStatusCode();

                var payload = await response.Content.ReadFromJsonAsync<PagedResultDto<PoiMobileDto>>(cancellationToken: cancellationToken);
                var pageItems = payload?.Items?.ToList() ?? [];
                if (pageItems.Count == 0)
                {
                    break;
                }

                items.AddRange(pageItems);
                if (payload is null || items.Count >= payload.TotalCount)
                {
                    break;
                }

                pageNumber++;
            }

            return items;
        }
        catch (OperationCanceledException)
        {
            return [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<PoiMobileDto?> GetPoiAsync(int id, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = string.IsNullOrWhiteSpace(languageCode)
                ? $"api/pois/{id}"
                : $"api/pois/{id}?lang={Uri.EscapeDataString(languageCode)}";
            _logger?.LogInformation("Calling GetPoiAsync for id={Id} endpoint={Endpoint}", id, endpoint);
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var raw = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger?.LogWarning("GetPoiAsync failed for id={Id} status={Status} body={Body}", id, response.StatusCode, raw);
                return null;
            }

            var dto = await response.Content.ReadFromJsonAsync<PoiMobileDto>(cancellationToken: cancellationToken);
            _logger?.LogInformation("GetPoiAsync success for id={Id}", id);
            return dto;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            _logger?.LogError("GetPoiAsync HttpRequestException for id={Id}", id);
            return null;
        }
    }

    public async Task<PoiMobileDto> CreatePoiAsync(UpsertPoiRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/pois", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var raw = await response.Content.ReadAsStringAsync(cancellationToken);
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(raw);
                    if (doc.RootElement.TryGetProperty("message", out var msg))
                    {
                        throw new InvalidOperationException(msg.GetString() ?? "API returned error");
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // ignore parse error and throw raw
                }

                throw new InvalidOperationException(string.IsNullOrWhiteSpace(raw) ? "API returned non-success status" : raw);
            }

            return (await response.Content.ReadFromJsonAsync<PoiMobileDto>(cancellationToken: cancellationToken))!;
        }
        catch (HttpRequestException)
        {
            return null!;
        }
    }

    public async Task<bool> UpdatePoiAsync(int id, UpsertPoiRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/pois/{id}", request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<bool> DeletePoiAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/pois/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<TravelApp.Application.Dtos.Metrics.MetricsOverviewDto?> GetMetricsOverviewAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TravelApp.Application.Dtos.Metrics.MetricsOverviewDto>("api/metrics/admin/overview", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<TravelApp.Application.Dtos.Metrics.EventAdminDto>> GetRecentEventsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = $"api/metrics/admin/recent?limit={limit}";
            return await _httpClient.GetFromJsonAsync<List<TravelApp.Application.Dtos.Metrics.EventAdminDto>>(endpoint, cancellationToken) ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<IReadOnlyList<UserAdminDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<UserAdminDto>>("api/admin/users", cancellationToken) ?? [];
        }
        catch (OperationCanceledException)
        {
            return [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<UserAdminDto?> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserAdminDto>($"api/admin/users/{id}", cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<RoleAdminDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<RoleAdminDto>>("api/admin/users/roles", cancellationToken) ?? [];
        }
        catch (OperationCanceledException)
        {
            return [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<UserAdminDto?> CreateUserAsync(UpsertUserRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/admin/users", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return (await response.Content.ReadFromJsonAsync<UserAdminDto>(cancellationToken: cancellationToken))!;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<bool> UpdateUserAsync(Guid id, UpsertUserRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/admin/users/{id}", request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/admin/users/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<TourAdminDto>> GetToursAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<TourAdminDto>>("api/admin/tours", cancellationToken) ?? [];
        }
        catch (OperationCanceledException)
        {
            return [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<TourAdminDto?> GetTourAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TourAdminDto>($"api/admin/tours/{id}", cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<TourAdminDto> CreateTourAsync(UpsertTourRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/admin/tours", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<TourAdminDto>(cancellationToken: cancellationToken))!;
        }
        catch (HttpRequestException)
        {
            return null!;
        }
    }

    public async Task<bool> UpdateTourAsync(int id, UpsertTourRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/admin/tours/{id}", request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<bool> DeleteTourAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/admin/tours/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
    
    public async Task<DashboardStatsDto?> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Gọi endpoint tổng hợp: PoiCount, UserCount, PublishedTourCount (Audio), QrCount
            return await _httpClient.GetFromJsonAsync<DashboardStatsDto>("api/admin/dashboard-stats", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<List<PoiStatDto>> GetPoiStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Gọi endpoint lấy danh sách Top POI kèm lượt Audio/QR
            return await _httpClient.GetFromJsonAsync<List<PoiStatDto>>("api/admin/poi-stats", cancellationToken) ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }
    public async Task<List<OnlineUserDisplayDto>> GetActiveUsersAsync(CancellationToken ct)
{
    return await _httpClient.GetFromJsonAsync<List<OnlineUserDisplayDto>>("/api/admin/active-users", ct) 
           ?? new List<OnlineUserDisplayDto>();
}

}

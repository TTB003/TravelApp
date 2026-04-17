using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TravelApp.Application.Dtos.Pois;

namespace TravelApp.Admin.Web.Services;

public class PoiApiService : IPoiApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PoiApiService> _logger;

    public PoiApiService(HttpClient httpClient, ILogger<PoiApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PoiMobileDto?> GetPoiAsync(int id, string? language = "vi", CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/pois/{id}?lang={language}";
            var resp = await _httpClient.GetFromJsonAsync<PoiMobileDto?>(url, cancellationToken);
            return resp;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to fetch POI {PoiId} from API", id);
            return null;
        }
    }
}

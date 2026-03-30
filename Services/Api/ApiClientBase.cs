using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Api;

public abstract class ApiClientBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiClientOptions _options;
    private readonly ITokenStore _tokenStore;

    protected ApiClientBase(IHttpClientFactory httpClientFactory, ApiClientOptions options, ITokenStore tokenStore)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _tokenStore = tokenStore;
    }

    protected static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    protected HttpClient CreateClient(bool authorized = false)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.BaseUrl);

        if (authorized && !string.IsNullOrWhiteSpace(_tokenStore.AccessToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(_tokenStore.TokenType, _tokenStore.AccessToken);
        }

        return client;
    }

    protected static async Task<T?> ReadAsAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
            return default;

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }
}

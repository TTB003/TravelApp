using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TravelApp.Application.Abstractions;

namespace TravelApp.Infrastructure.Services.Translation;

public class GoogleTranslationService : ITranslationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleTranslationService> _logger;

    public GoogleTranslationService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GoogleTranslationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> TranslateTextAsync(string text, string targetLanguage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var apiKey = _configuration["Google:TranslateApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Google Translate API key is not configured.");
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var requestUrl = $"https://translation.googleapis.com/language/translate/v2?key={Uri.EscapeDataString(apiKey)}";
            var payload = new
            {
                q = new[] { text },
                target = targetLanguage,
                format = "text"
            };

            var response = await client.PostAsJsonAsync(requestUrl, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Translate returned status {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<GoogleTranslateResponse>(cancellationToken: cancellationToken);
            var translated = json?.Data?.Translations?.FirstOrDefault()?.TranslatedText;
            return translated;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google Translate call failed.");
            return null;
        }
    }

    private class GoogleTranslateResponse
    {
        public GoogleData? Data { get; set; }
    }

    private class GoogleData
    {
        public List<GoogleTranslation>? Translations { get; set; }
    }

    private class GoogleTranslation
    {
        public string? TranslatedText { get; set; }
    }
}

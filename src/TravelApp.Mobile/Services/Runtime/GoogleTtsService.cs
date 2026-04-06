using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public class GoogleTtsService : ITextToSpeechService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAudioManager _audioManager;
    private readonly ILogger<GoogleTtsService> _logger;

    public GoogleTtsService(IHttpClientFactory httpClientFactory, IAudioManager audioManager, ILogger<GoogleTtsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _audioManager = audioManager;
        _logger = logger;
    }

    // Skeleton implementation: expects a POST /tts?lang={lang} that returns audio bytes
    public async Task PlayTextAsync(string text, string languageCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            var client = _httpClientFactory.CreateClient();
            var endpoint = $"tts?lang={Uri.EscapeDataString(languageCode)}";
            var request = JsonContent.Create(new { text });
            var response = await client.PostAsync(endpoint, request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TTS server returned non-success status {Status}", response.StatusCode);
                return;
            }

            await using var ms = new MemoryStream();
            await response.Content.CopyToAsync(ms, cancellationToken);
            ms.Seek(0, SeekOrigin.Begin);

            var player = _audioManager.CreatePlayer(ms);
            player.Play();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GoogleTtsService: failed to play text");
        }
    }
}

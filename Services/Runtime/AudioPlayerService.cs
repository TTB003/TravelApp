using Plugin.Maui.Audio;
using Microsoft.Extensions.Logging;
using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public class AudioPlayerService : IAudioPlayerService
{
    private readonly IAudioManager _audioManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogService _logService;
    private readonly ILogger<AudioPlayerService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IAudioPlayer? _player;
    private Stream? _activeStream;

    public bool IsPlaying { get; private set; }
    public int? CurrentPoiId { get; private set; }
    public string? CurrentPoiTitle { get; private set; }

    public AudioPlayerService(IAudioManager audioManager, IHttpClientFactory httpClientFactory, ILogService logService, ILogger<AudioPlayerService> logger)
    {
        _audioManager = audioManager;
        _httpClientFactory = httpClientFactory;
        _logService = logService;
        _logger = logger;
    }

    public async Task PlayAsync(AudioTriggerRequest request, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (IsPlaying && CurrentPoiId == request.Poi.Id)
            {
                _logger.LogDebug("Audio play ignored: POI {PoiId} ({PoiTitle}) already playing.", request.Poi.Id, request.Poi.Title);
                return;
            }

            if (IsPlaying && CurrentPoiId != request.Poi.Id)
            {
                await StopInternalAsync($"switch-poi:{request.Poi.Id}");
            }
            else
            {
                await StopInternalAsync("prepare-play");
            }

            var source = ResolveSource(request);
            if (source is null)
            {
                _logger.LogWarning("Audio source not found for POI {PoiId} ({PoiTitle}).", request.Poi.Id, request.Poi.Title);
                return;
            }

            var stream = await OpenStreamAsync(source, cancellationToken);
            if (stream is null)
            {
                _logger.LogWarning("Audio stream open failed for POI {PoiId} ({PoiTitle}).", request.Poi.Id, request.Poi.Title);
                return;
            }

            var player = _audioManager.CreatePlayer(stream);
            player.PlaybackEnded += OnPlaybackEnded;
            player.Play();

            _activeStream = stream;
            _player = player;
            IsPlaying = true;
            CurrentPoiId = request.Poi.Id;
            CurrentPoiTitle = request.Poi.Title;
            _logger.LogInformation(
                "Audio started: POI {PoiId} ({PoiTitle}), source={SourceType}.",
                request.Poi.Id,
                request.Poi.Title,
                string.IsNullOrWhiteSpace(source.LocalFilePath) ? "streaming" : "offline-cache");
            _logService.Log("Audio", $"PLAY poi={request.Poi.Id} ({request.Poi.Title}) source={(string.IsNullOrWhiteSpace(source.LocalFilePath) ? "streaming" : "offline-cache")}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audio playback failed for POI {PoiId} ({PoiTitle}).", request.Poi.Id, request.Poi.Title);
            await StopInternalAsync("play-error");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await StopInternalAsync("manual-stop");
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task StopInternalAsync(string reason)
    {
        var wasPlaying = IsPlaying;
        var poiId = CurrentPoiId;
        var poiTitle = CurrentPoiTitle;

        if (_player is not null)
        {
            _player.PlaybackEnded -= OnPlaybackEnded;
            _player.Stop();
            _player.Dispose();
            _player = null;
        }

        if (_activeStream is not null)
        {
            await _activeStream.DisposeAsync();
            _activeStream = null;
        }

        IsPlaying = false;
        CurrentPoiId = null;
        CurrentPoiTitle = null;

        if (wasPlaying)
        {
            _logger.LogInformation("Audio stopped: POI {PoiId} ({PoiTitle}), reason={Reason}.", poiId, poiTitle, reason);
            _logService.Log("Audio", $"STOP poi={poiId} ({poiTitle}) reason={reason}");
        }
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        _ = Task.Run(async () =>
        {
            await _gate.WaitAsync();
            try
            {
                await StopInternalAsync("playback-ended");
            }
            finally
            {
                _gate.Release();
            }
        });
    }

    private async Task<Stream?> OpenStreamAsync(ResolvedAudioSource source, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(source.LocalFilePath) && File.Exists(source.LocalFilePath))
        {
            return File.OpenRead(source.LocalFilePath);
        }

        if (string.IsNullOrWhiteSpace(source.StreamingUrl))
        {
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            return await client.GetStreamAsync(source.StreamingUrl, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static ResolvedAudioSource? ResolveSource(AudioTriggerRequest request)
    {
        var streamingUrl = SelectStreamingUrl(request.Poi, request.LanguageCode);
        var localFilePath = ResolveCachedFilePath(request.Poi.Id, request.LanguageCode, streamingUrl);

        if (string.IsNullOrWhiteSpace(localFilePath) && string.IsNullOrWhiteSpace(streamingUrl))
        {
            return null;
        }

        return new ResolvedAudioSource(localFilePath, streamingUrl);
    }

    private static string? SelectStreamingUrl(PoiDto poi, string languageCode)
    {
        static string? FirstUrl(IEnumerable<PoiAudioDto> assets)
            => assets.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.AudioUrl))?.AudioUrl;

        var byRequestedLanguage = FirstUrl(poi.AudioAssets.Where(x => string.Equals(x.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(byRequestedLanguage))
        {
            return byRequestedLanguage;
        }

        if (!string.IsNullOrWhiteSpace(poi.PrimaryLanguage))
        {
            var byPrimaryLanguage = FirstUrl(poi.AudioAssets.Where(x => string.Equals(x.LanguageCode, poi.PrimaryLanguage, StringComparison.OrdinalIgnoreCase)));
            if (!string.IsNullOrWhiteSpace(byPrimaryLanguage))
            {
                return byPrimaryLanguage;
            }
        }

        return FirstUrl(poi.AudioAssets);
    }

    private static string? ResolveCachedFilePath(int poiId, string languageCode, string? streamingUrl)
    {
        var cacheDirectory = Path.Combine(FileSystem.CacheDirectory, "audio");
        var candidates = new List<string>
        {
            Path.Combine(cacheDirectory, $"poi-{poiId}-{languageCode}.mp3")
        };

        if (!string.IsNullOrWhiteSpace(streamingUrl) && Uri.TryCreate(streamingUrl, UriKind.Absolute, out var uri))
        {
            var extension = Path.GetExtension(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                candidates.Add(Path.Combine(cacheDirectory, $"poi-{poiId}-{languageCode}{extension}"));
            }

            var fileName = Path.GetFileName(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                candidates.Add(Path.Combine(cacheDirectory, fileName));
            }
        }

        return candidates.FirstOrDefault(File.Exists);
    }

    private sealed record ResolvedAudioSource(string? LocalFilePath, string? StreamingUrl);
}

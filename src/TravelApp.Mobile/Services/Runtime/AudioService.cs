using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using TravelApp.Models.Contracts;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public class AudioService : IAudioService, IDisposable
{
    public event EventHandler? PlaybackEnded;

    private readonly IPoiGeofenceService _poiGeofenceService;
    private readonly IAudioManager _audioManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly ILogService _logService;
    private readonly ILogger<AudioService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IAudioPlayer? _player;
    private Stream? _activeStream;
    private CancellationTokenSource? _ttsPlaybackCts;

    public AudioService(
        IPoiGeofenceService poiGeofenceService,
        IAudioManager audioManager,
        IHttpClientFactory httpClientFactory,
        ILocalDatabaseService localDatabaseService,
        ILogService logService,
        ILogger<AudioService> logger)
    {
        _poiGeofenceService = poiGeofenceService;
        _audioManager = audioManager;
        _httpClientFactory = httpClientFactory;
        _localDatabaseService = localDatabaseService;
        _logService = logService;
        _logger = logger;

        _poiGeofenceService.OnPoiEntered += OnPoiEntered;
    }

    public async Task PlayPoiAudioAsync(PoiMobileDto poi, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await StopInternalAsync("switch-poi");

            var languageCode = string.IsNullOrWhiteSpace(poi.LanguageCode)
                ? (string.IsNullOrWhiteSpace(poi.PrimaryLanguage) ? "en" : poi.PrimaryLanguage)
                : poi.LanguageCode;

            var speechText = BuildSpeechText(poi, languageCode);
            if (!string.IsNullOrWhiteSpace(speechText))
            {
                _ttsPlaybackCts?.Cancel();
                _ttsPlaybackCts?.Dispose();
                _ttsPlaybackCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                try
                {
                    await TextToSpeech.Default.SpeakAsync(speechText, new SpeechOptions
                    {
                        Locale = await ResolveLocaleAsync(languageCode)
                    }, _ttsPlaybackCts.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                PlaybackEnded?.Invoke(this, EventArgs.Empty);
                LogSource("local-tts", poi, languageCode, speechText);
                return;
            }

            var offlineSource = await ResolveOfflineAudioSourceAsync(poi, languageCode, cancellationToken);
            if (!string.IsNullOrWhiteSpace(offlineSource) && File.Exists(offlineSource))
            {
                var fileStream = File.OpenRead(offlineSource);
                StartPlayer(fileStream);
                LogSource("offline-local", poi, languageCode, offlineSource);
                return;
            }

            var preGeneratedUrl = SelectPreGeneratedAudioUrl(poi, languageCode);
            if (!string.IsNullOrWhiteSpace(preGeneratedUrl))
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    var localPath = BuildOfflineAudioPath(poi.Id, languageCode, preGeneratedUrl);
                    var bytes = await client.GetByteArrayAsync(preGeneratedUrl, cancellationToken);
                    Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                    await File.WriteAllBytesAsync(localPath, bytes, cancellationToken);

                    await _localDatabaseService.SaveAudioMetadataAsync(poi.Id, languageCode, preGeneratedUrl, localPath, cancellationToken);

                    var stream = File.OpenRead(localPath);
                    StartPlayer(stream);
                    LogSource("pre-generated-server-cached", poi, languageCode, localPath);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Pre-generated audio unavailable for POI {PoiId} ({PoiTitle}).", poi.Id, poi.Title);
                }
            }

            if (await TryPlayCloudTtsSimulatedAsync(poi, languageCode, cancellationToken))
            {
                LogSource("cloud-tts-simulated", poi, languageCode, "simulated");
                return;
            }

            await PlayWithLocalTtsAsync(poi, languageCode, cancellationToken);
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
            LogSource("local-tts", poi, languageCode, "TextToSpeech.Default");
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
            _ttsPlaybackCts?.Cancel();
            await StopInternalAsync("manual-stop");
            _ttsPlaybackCts?.Dispose();
            _ttsPlaybackCts = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async void OnPoiEntered(PoiMobileDto poi)
    {
        try
        {
            await PlayPoiAudioAsync(poi);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Play POI audio failed for POI {PoiId} ({PoiTitle}).", poi.Id, poi.Title);
        }
    }

    private async Task StopInternalAsync(string reason)
    {
        _ttsPlaybackCts?.Cancel();

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

        _logger.LogDebug("Audio service stop, reason={Reason}.", reason);
    }

    private void StartPlayer(Stream stream)
    {
        var player = _audioManager.CreatePlayer(stream);
        player.PlaybackEnded += OnPlaybackEnded;
        player.Play();

        _activeStream = stream;
        _player = player;
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        PlaybackEnded?.Invoke(this, EventArgs.Empty);

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

    private async Task<string?> ResolveOfflineAudioSourceAsync(PoiMobileDto poi, string languageCode, CancellationToken cancellationToken)
    {
        var fromMetadata = await _localDatabaseService.GetOfflineAudioPathAsync(poi.Id, languageCode, cancellationToken);
        if (!string.IsNullOrWhiteSpace(fromMetadata) && File.Exists(fromMetadata))
        {
            return fromMetadata;
        }

        var cacheDirectory = Path.Combine(FileSystem.CacheDirectory, "audio");
        var selectedUrl = SelectPreGeneratedAudioUrl(poi, languageCode);

        var candidates = new List<string>
        {
            Path.Combine(cacheDirectory, $"poi-{poi.Id}-{languageCode}.mp3")
        };

        if (!string.IsNullOrWhiteSpace(selectedUrl) && Uri.TryCreate(selectedUrl, UriKind.Absolute, out var uri))
        {
            var extension = Path.GetExtension(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                candidates.Add(Path.Combine(cacheDirectory, $"poi-{poi.Id}-{languageCode}{extension}"));
            }

            var fileName = Path.GetFileName(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                candidates.Add(Path.Combine(cacheDirectory, fileName));
            }
        }

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string BuildOfflineAudioPath(int poiId, string languageCode, string? url)
    {
        var cacheDirectory = Path.Combine(FileSystem.CacheDirectory, "audio");
        var extension = ".mp3";

        if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var extFromUrl = Path.GetExtension(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(extFromUrl))
            {
                extension = extFromUrl;
            }
        }

        return Path.Combine(cacheDirectory, $"poi-{poiId}-{languageCode}{extension}");
    }

    private static string? SelectPreGeneratedAudioUrl(PoiMobileDto poi, string languageCode)
    {
        static string? FirstUrl(IEnumerable<PoiAudioMobileDto> assets)
            => assets.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.AudioUrl))?.AudioUrl;

        var byRequestedLang = FirstUrl(poi.AudioAssets.Where(x => string.Equals(x.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(byRequestedLang))
        {
            return byRequestedLang;
        }

        var byPrimaryLang = FirstUrl(poi.AudioAssets.Where(x => string.Equals(x.LanguageCode, poi.PrimaryLanguage, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(byPrimaryLang))
        {
            return byPrimaryLang;
        }

        return FirstUrl(poi.AudioAssets);
    }

    private static async Task<bool> TryPlayCloudTtsSimulatedAsync(PoiMobileDto poi, string languageCode, CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken);
        _ = poi;
        _ = languageCode;
        return false;
    }

    private static async Task PlayWithLocalTtsAsync(PoiMobileDto poi, string languageCode, CancellationToken cancellationToken)
    {
        var text = BuildSpeechText(poi, languageCode);
        if (string.IsNullOrWhiteSpace(text))
        {
            text = $"Bạn đang ở gần {poi.Title}";
        }

        await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions
        {
            Locale = await ResolveLocaleAsync(languageCode)
        }, cancellationToken);
    }

    private static async Task<Locale?> ResolveLocaleAsync(string languageCode)
    {
        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            return locales.FirstOrDefault(x => string.Equals(x.Language, languageCode, StringComparison.OrdinalIgnoreCase))
                   ?? locales.FirstOrDefault(x => x.Language.StartsWith(languageCode, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }

    private static string BuildSpeechText(PoiMobileDto poi, string languageCode)
    {
        if (!string.IsNullOrWhiteSpace(poi.SpeechText))
        {
            return poi.SpeechText;
        }

        if (!string.IsNullOrWhiteSpace(poi.Description))
        {
            return poi.Description;
        }

        if (!string.IsNullOrWhiteSpace(poi.Subtitle))
        {
            return $"{poi.Title}. {poi.Subtitle}";
        }

        return poi.Title;
    }

    private void LogSource(string source, PoiMobileDto poi, string languageCode, string detail)
    {
        _logger.LogInformation(
            "POI audio source={Source}, poi={PoiId} ({PoiTitle}), lang={Language}, detail={Detail}",
            source,
            poi.Id,
            poi.Title,
            languageCode,
            detail);

        _logService.Log("Audio", $"SOURCE={source} poi={poi.Id} ({poi.Title}) lang={languageCode} detail={detail}");
    }

    public void Dispose()
    {
        _poiGeofenceService.OnPoiEntered -= OnPoiEntered;
        _ttsPlaybackCts?.Cancel();
        _ttsPlaybackCts?.Dispose();
        _gate.Dispose();
    }
}

using System.Text.Json;
using Microsoft.Extensions.Logging;
using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public sealed class AudioLibraryService : IAudioLibraryService
{
    private const string QueueStatePreferenceKey = "audio_library_queue_v1";
    private const string FailedStatePreferenceKey = "audio_library_failed_v1";

    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly IPoiApiClient _poiApiClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAudioPlayerService _audioPlayerService;
    private readonly ILogger<AudioLibraryService> _logger;

    private readonly object _sync = new();
    private readonly List<DownloadQueueItem> _queue = [];
    private readonly HashSet<string> _queueKeys = [];
    private readonly HashSet<string> _failedKeys = [];

    private int _isQueueProcessing;
    private double _averageBytesPerSecond;
    private long _averageBytesPerDownload;
    private int _completedDownloads;

    public event EventHandler? LibraryChanged;
    public event EventHandler<AudioDownloadProgressChangedEventArgs>? DownloadProgressChanged;

    public AudioLibraryService(
        ILocalDatabaseService localDatabaseService,
        IPoiApiClient poiApiClient,
        IHttpClientFactory httpClientFactory,
        IAudioPlayerService audioPlayerService,
        ILogger<AudioLibraryService> logger)
    {
        _localDatabaseService = localDatabaseService;
        _poiApiClient = poiApiClient;
        _httpClientFactory = httpClientFactory;
        _audioPlayerService = audioPlayerService;
        _logger = logger;

        RestoreQueueState();
        _ = ProcessQueueAsync(CancellationToken.None);
    }

    public async Task<IReadOnlyList<AudioLibraryItem>> GetLibraryItemsAsync(string? languageCode, CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = NormalizeLanguage(languageCode);
        var pois = await EnsureCatalogueAsync(normalizedLanguage, cancellationToken);

        var items = new List<AudioLibraryItem>(pois.Count);
        HashSet<int> queuedPoiIds;
        lock (_sync)
        {
            queuedPoiIds = _queue.Select(x => x.PoiId).ToHashSet();
        }

        foreach (var poi in pois)
        {
            var path = await _localDatabaseService.GetOfflineAudioPathAsync(poi.Id, normalizedLanguage, cancellationToken);
            var downloaded = !string.IsNullOrWhiteSpace(path) && File.Exists(path);
            var size = downloaded ? new FileInfo(path!).Length : 0;

            items.Add(new AudioLibraryItem
            {
                PoiId = poi.Id,
                Title = poi.Title,
                Subtitle = poi.Subtitle,
                Location = poi.Location,
                ImageUrl = poi.ImageUrl,
                LanguageCode = normalizedLanguage,
                AudioUrl = SelectAudioUrl(poi, normalizedLanguage),
                IsDownloaded = downloaded,
                LocalFilePath = path,
                FileSizeBytes = size,
                IsBusy = queuedPoiIds.Contains(poi.Id),
                DownloadProgress = 0,
                DownloadStatusText = queuedPoiIds.Contains(poi.Id) ? "Đang chờ tải..." : string.Empty
            });
        }

        return items
            .OrderByDescending(x => x.IsDownloaded)
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<bool> DownloadAsync(int poiId, string? languageCode, CancellationToken cancellationToken = default)
    {
        var queued = await EnqueueDownloadsAsync([poiId], languageCode, cancellationToken);
        return queued > 0;
    }

    public Task<int> DownloadManyAsync(IEnumerable<int> poiIds, string? languageCode, CancellationToken cancellationToken = default)
    {
        return EnqueueDownloadsAsync(poiIds, languageCode, cancellationToken);
    }

    public Task<int> EnqueueDownloadsAsync(IEnumerable<int> poiIds, string? languageCode, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        var normalizedLanguage = NormalizeLanguage(languageCode);
        var queuedCount = 0;

        lock (_sync)
        {
            foreach (var poiId in poiIds.Distinct())
            {
                var key = BuildStateKey(poiId, normalizedLanguage);
                if (!_queueKeys.Add(key))
                {
                    continue;
                }

                _queue.Add(new DownloadQueueItem(poiId, normalizedLanguage));
                _failedKeys.Remove(key);
                queuedCount++;
            }

            PersistStateLocked();
        }

        if (queuedCount > 0)
        {
            EmitQueueStatus("Đã thêm vào hàng chờ tải.");
            _ = ProcessQueueAsync(CancellationToken.None);
        }

        return Task.FromResult(queuedCount);
    }

    public Task<int> RetryFailedAsync(string? languageCode, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        var normalizedLanguage = NormalizeLanguage(languageCode);
        List<DownloadQueueItem> retryTargets;

        lock (_sync)
        {
            retryTargets = _failedKeys
                .Select(ParseStateKey)
                .Where(x => x is not null)
                .Select(x => x!)
                .Where(x => string.Equals(x.LanguageCode, normalizedLanguage, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var item in retryTargets)
            {
                _failedKeys.Remove(BuildStateKey(item.PoiId, item.LanguageCode));
            }

            PersistStateLocked();
        }

        if (retryTargets.Count == 0)
        {
            return Task.FromResult(0);
        }

        return EnqueueDownloadsAsync(retryTargets.Select(x => x.PoiId), normalizedLanguage, cancellationToken);
    }

    public async Task<bool> RemoveDownloadAsync(int poiId, string? languageCode, CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = NormalizeLanguage(languageCode);
        var existingPath = await _localDatabaseService.GetOfflineAudioPathAsync(poiId, normalizedLanguage, cancellationToken);
        if (string.IsNullOrWhiteSpace(existingPath))
        {
            return false;
        }

        try
        {
            if (File.Exists(existingPath))
            {
                File.Delete(existingPath);
            }

            var pois = await EnsureCatalogueAsync(normalizedLanguage, cancellationToken);
            var poi = pois.FirstOrDefault(x => x.Id == poiId);
            var audioUrl = poi is null ? null : SelectAudioUrl(poi, normalizedLanguage);
            await _localDatabaseService.SaveAudioMetadataAsync(poiId, normalizedLanguage, audioUrl, null, cancellationToken);

            EmitProgress(poiId, 0, isCompleted: true, isFailed: false, "Đã xóa offline.");
            LibraryChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audio library remove failed for POI {PoiId}.", poiId);
            return false;
        }
    }

    public async Task<bool> PlayAsync(int poiId, string? languageCode, CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = NormalizeLanguage(languageCode);
        var pois = await EnsureCatalogueAsync(normalizedLanguage, cancellationToken);
        var poi = pois.FirstOrDefault(x => x.Id == poiId);

        if (poi is null)
        {
            return false;
        }

        await _audioPlayerService.PlayAsync(new AudioTriggerRequest(
            ToContractPoi(poi),
            new LocationSample(poi.Latitude, poi.Longitude, DateTimeOffset.UtcNow),
            normalizedLanguage,
            DateTimeOffset.UtcNow), cancellationToken);
        return true;
    }

    public async Task<int> GetDownloadedCountAsync(string? languageCode, CancellationToken cancellationToken = default)
    {
        var items = await GetLibraryItemsAsync(languageCode, cancellationToken);
        return items.Count(x => x.IsDownloaded);
    }

    public Task<int> GetFailedCountAsync(string? languageCode, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var normalizedLanguage = NormalizeLanguage(languageCode);

        lock (_sync)
        {
            var count = _failedKeys
                .Select(ParseStateKey)
                .Where(x => x is not null && string.Equals(x.LanguageCode, normalizedLanguage, StringComparison.OrdinalIgnoreCase))
                .Count();

            return Task.FromResult(count);
        }
    }

    public Task<int> GetPendingQueueCountAsync(string? languageCode, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var normalizedLanguage = NormalizeLanguage(languageCode);

        lock (_sync)
        {
            var count = _queue.Count(x => string.Equals(x.LanguageCode, normalizedLanguage, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(count);
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _isQueueProcessing, 1) == 1)
        {
            return;
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                DownloadQueueItem? current;
                int pendingCount;

                lock (_sync)
                {
                    current = _queue.FirstOrDefault();
                    pendingCount = _queue.Count;
                }

                if (current is null)
                {
                    break;
                }

                EmitProgress(current.PoiId, 0, isCompleted: false, isFailed: false, "Đang bắt đầu tải...", pendingCount, null);

                var success = await DownloadInternalAsync(current, cancellationToken);

                lock (_sync)
                {
                    var key = BuildStateKey(current.PoiId, current.LanguageCode);
                    _queue.Remove(current);
                    _queueKeys.Remove(key);

                    if (!success)
                    {
                        _failedKeys.Add(key);
                    }

                    PersistStateLocked();
                }

                LibraryChanged?.Invoke(this, EventArgs.Empty);
                EmitQueueStatus(success ? "Đã hoàn tất 1 mục tải." : "Có mục tải thất bại.");
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isQueueProcessing, 0);

            lock (_sync)
            {
                if (_queue.Count > 0)
                {
                    _ = ProcessQueueAsync(CancellationToken.None);
                }
            }
        }
    }

    private async Task<bool> DownloadInternalAsync(DownloadQueueItem item, CancellationToken cancellationToken)
    {
        var pois = await EnsureCatalogueAsync(item.LanguageCode, cancellationToken);
        var poi = pois.FirstOrDefault(x => x.Id == item.PoiId);
        if (poi is null)
        {
            EmitProgress(item.PoiId, 0, isCompleted: true, isFailed: true, "Không tìm thấy POI.", GetQueueCount(), null);
            return false;
        }

        var audioUrl = SelectAudioUrl(poi, item.LanguageCode);
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            EmitProgress(item.PoiId, 0, isCompleted: true, isFailed: true, "POI chưa có audio.", GetQueueCount(), null);
            return false;
        }

        var localPath = BuildOfflineAudioPath(item.PoiId, item.LanguageCode, audioUrl);

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync(audioUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                EmitProgress(item.PoiId, 0, isCompleted: true, isFailed: true, "Không tải được audio.", GetQueueCount(), null);
                return false;
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var fileStream = File.Create(localPath);
            var buffer = new byte[16 * 1024];
            var contentLength = response.Content.Headers.ContentLength;
            long totalRead = 0;
            var startedAt = DateTimeOffset.UtcNow;

            while (true)
            {
                var read = await responseStream.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    break;
                }

                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                totalRead += read;

                if (contentLength.HasValue && contentLength.Value > 0)
                {
                    var progress = Math.Min(1d, (double)totalRead / contentLength.Value);
                    var eta = ComputeEstimatedRemaining(contentLength.Value, totalRead, startedAt, item);
                    EmitProgress(item.PoiId, progress, isCompleted: false, isFailed: false, null, GetQueueCount(), eta);
                }
            }

            await _localDatabaseService.SaveAudioMetadataAsync(item.PoiId, item.LanguageCode, audioUrl, localPath, cancellationToken);
            UpdateDownloadAverages(totalRead, startedAt);

            EmitProgress(item.PoiId, 1, isCompleted: true, isFailed: false, "Đã tải offline.", Math.Max(0, GetQueueCount() - 1), TimeSpan.Zero);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audio library download failed for POI {PoiId}.", item.PoiId);

            try
            {
                if (File.Exists(localPath))
                {
                    File.Delete(localPath);
                }
            }
            catch
            {
            }

            EmitProgress(item.PoiId, 0, isCompleted: true, isFailed: true, "Tải thất bại.", Math.Max(0, GetQueueCount() - 1), null);
            return false;
        }
    }

    private TimeSpan? ComputeEstimatedRemaining(long totalBytes, long currentRead, DateTimeOffset startedAt, DownloadQueueItem currentItem)
    {
        var elapsedSeconds = Math.Max(0.1, (DateTimeOffset.UtcNow - startedAt).TotalSeconds);
        var currentSpeed = currentRead / elapsedSeconds;
        if (currentSpeed <= 1)
        {
            currentSpeed = _averageBytesPerSecond > 1 ? _averageBytesPerSecond : 64 * 1024;
        }

        var currentRemainingBytes = Math.Max(0, totalBytes - currentRead);
        var currentRemainingSeconds = currentRemainingBytes / currentSpeed;

        int remainingQueueCount;
        lock (_sync)
        {
            remainingQueueCount = _queue.Count(x => !(x.PoiId == currentItem.PoiId && string.Equals(x.LanguageCode, currentItem.LanguageCode, StringComparison.OrdinalIgnoreCase))) - 1;
            if (remainingQueueCount < 0)
            {
                remainingQueueCount = 0;
            }
        }

        var averageBytes = _averageBytesPerDownload > 0 ? _averageBytesPerDownload : totalBytes;
        var averageSpeed = _averageBytesPerSecond > 1 ? _averageBytesPerSecond : currentSpeed;

        var queuedRemainingSeconds = remainingQueueCount * (averageBytes / averageSpeed);
        var totalSeconds = currentRemainingSeconds + queuedRemainingSeconds;
        return TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
    }

    private void UpdateDownloadAverages(long bytesDownloaded, DateTimeOffset startedAt)
    {
        var seconds = Math.Max(0.1, (DateTimeOffset.UtcNow - startedAt).TotalSeconds);
        var speed = bytesDownloaded / seconds;

        _completedDownloads++;

        if (_completedDownloads == 1)
        {
            _averageBytesPerDownload = bytesDownloaded;
            _averageBytesPerSecond = speed;
            return;
        }

        _averageBytesPerDownload = (long)((_averageBytesPerDownload * (_completedDownloads - 1) + bytesDownloaded) / (double)_completedDownloads);
        _averageBytesPerSecond = (_averageBytesPerSecond * (_completedDownloads - 1) + speed) / _completedDownloads;
    }

    private async Task<IReadOnlyList<PoiMobileDto>> EnsureCatalogueAsync(string languageCode, CancellationToken cancellationToken)
    {
        var local = await _localDatabaseService.GetPoisAsync(languageCode, cancellationToken: cancellationToken);
        if (local.Count > 0)
        {
            return local;
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            return [];
        }

        try
        {
            var remote = await _poiApiClient.GetAllAsync(languageCode, cancellationToken);
            var mapped = remote.Select(x => new PoiMobileDto
            {
                Id = x.Id,
                Title = x.Title,
                Subtitle = x.Subtitle,
                Description = x.Description ?? string.Empty,
                SpeechText = x.SpeechText,
                LanguageCode = NormalizeLanguage(x.PrimaryLanguage),
                PrimaryLanguage = NormalizeLanguage(x.PrimaryLanguage),
                ImageUrl = x.ImageUrl,
                Location = x.Location,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                GeofenceRadiusMeters = x.GeofenceRadiusMeters ?? 200,
                Category = x.Category ?? string.Empty,
                AudioAssets = x.AudioAssets.Select(a => new PoiAudioMobileDto
                {
                    LanguageCode = NormalizeLanguage(a.LanguageCode),
                    AudioUrl = a.AudioUrl,
                    Transcript = a.Transcript,
                    IsGenerated = a.IsGenerated
                }).ToList()
            }).ToList();

            await _localDatabaseService.SavePoisAsync(mapped, cancellationToken);
            return mapped;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audio library could not sync POI catalogue from API.");
            return [];
        }
    }

    private static string? SelectAudioUrl(PoiMobileDto poi, string languageCode)
    {
        static string? FirstUrl(IEnumerable<PoiAudioMobileDto> assets)
            => assets.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.AudioUrl))?.AudioUrl;

        var byRequested = FirstUrl(poi.AudioAssets.Where(x => string.Equals(x.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(byRequested))
        {
            return byRequested;
        }

        var byPrimary = FirstUrl(poi.AudioAssets.Where(x => string.Equals(x.LanguageCode, poi.PrimaryLanguage, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(byPrimary))
        {
            return byPrimary;
        }

        return FirstUrl(poi.AudioAssets);
    }

    private static PoiDto ToContractPoi(PoiMobileDto poi)
    {
        return new PoiDto
        {
            Id = poi.Id,
            Title = poi.Title,
            Subtitle = poi.Subtitle,
            Description = poi.Description,
            Category = poi.Category,
            ImageUrl = poi.ImageUrl,
            Location = poi.Location,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            GeofenceRadiusMeters = poi.GeofenceRadiusMeters,
            Duration = string.Empty,
            Provider = null,
            Credit = null,
            PrimaryLanguage = poi.PrimaryLanguage,
            SpeechText = poi.SpeechText,
            AudioAssets = poi.AudioAssets.Select(audio => new PoiAudioDto(audio.LanguageCode, audio.AudioUrl, audio.Transcript, audio.IsGenerated)).ToList(),
            Localizations = []
        };
    }

    private static string BuildOfflineAudioPath(int poiId, string languageCode, string? url)
    {
        var cacheDirectory = Path.Combine(FileSystem.CacheDirectory, "audio");
        var extension = ".mp3";

        if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var ext = Path.GetExtension(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(ext))
            {
                extension = ext;
            }
        }

        return Path.Combine(cacheDirectory, $"poi-{poiId}-{languageCode}{extension}");
    }

    private static string NormalizeLanguage(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? "en"
            : languageCode.Trim().ToLowerInvariant();
    }

    private void EmitProgress(int poiId, double progress, bool isCompleted, bool isFailed, string? message, int pendingQueueCount = 0, TimeSpan? eta = null)
    {
        DownloadProgressChanged?.Invoke(this, new AudioDownloadProgressChangedEventArgs(poiId, progress, isCompleted, isFailed, message, pendingQueueCount, eta));
    }

    private void EmitQueueStatus(string message)
    {
        DownloadProgressChanged?.Invoke(this, new AudioDownloadProgressChangedEventArgs(0, 0, false, false, message, GetQueueCount(), null));
    }

    private int GetQueueCount()
    {
        lock (_sync)
        {
            return _queue.Count;
        }
    }

    private void RestoreQueueState()
    {
        lock (_sync)
        {
            var queueRaw = Preferences.Default.Get(QueueStatePreferenceKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(queueRaw))
            {
                try
                {
                    var keys = JsonSerializer.Deserialize<List<string>>(queueRaw) ?? [];
                    foreach (var key in keys)
                    {
                        var item = ParseStateKey(key);
                        if (item is null)
                        {
                            continue;
                        }

                        var normalizedKey = BuildStateKey(item.PoiId, item.LanguageCode);
                        if (!_queueKeys.Add(normalizedKey))
                        {
                            continue;
                        }

                        _queue.Add(item);
                    }
                }
                catch
                {
                }
            }

            var failedRaw = Preferences.Default.Get(FailedStatePreferenceKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(failedRaw))
            {
                try
                {
                    var failed = JsonSerializer.Deserialize<List<string>>(failedRaw) ?? [];
                    foreach (var key in failed)
                    {
                        _failedKeys.Add(key);
                    }
                }
                catch
                {
                }
            }
        }
    }

    private void PersistStateLocked()
    {
        var queueKeys = _queue.Select(x => BuildStateKey(x.PoiId, x.LanguageCode)).ToList();
        Preferences.Default.Set(QueueStatePreferenceKey, JsonSerializer.Serialize(queueKeys));
        Preferences.Default.Set(FailedStatePreferenceKey, JsonSerializer.Serialize(_failedKeys.ToList()));
    }

    private static string BuildStateKey(int poiId, string languageCode)
    {
        return $"{poiId}|{NormalizeLanguage(languageCode)}";
    }

    private static DownloadQueueItem? ParseStateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var parts = key.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return null;
        }

        if (!int.TryParse(parts[0], out var poiId))
        {
            return null;
        }

        return new DownloadQueueItem(poiId, NormalizeLanguage(parts[1]));
    }

    private sealed record DownloadQueueItem(int PoiId, string LanguageCode);
}

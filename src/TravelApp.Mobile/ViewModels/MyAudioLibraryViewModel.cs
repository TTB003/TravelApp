using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Models.Runtime;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public sealed class MyAudioLibraryViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IAudioLibraryService _audioLibraryService;
    private readonly IAudioPlayerService _audioPlayerService;
    private readonly List<AudioLibraryItem> _allItems = [];

    private bool _isLoading;
    private string _statusText = "Đang tải thư viện...";
    private string _activeFilter = "All";
    private string _queueEtaText = "Queue idle";
    private int _failedCount;

    public ObservableCollection<AudioLibraryItem> Items { get; } = [];

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
            {
                return;
            }

            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public string QueueEtaText
    {
        get => _queueEtaText;
        private set
        {
            if (_queueEtaText == value)
            {
                return;
            }

            _queueEtaText = value;
            OnPropertyChanged();
        }
    }

    public int FailedCount
    {
        get => _failedCount;
        private set
        {
            if (_failedCount == value)
            {
                return;
            }

            _failedCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasFailedDownloads));
            OnPropertyChanged(nameof(RetryFailedText));
        }
    }

    public bool HasFailedDownloads => FailedCount > 0;

    public string RetryFailedText => FailedCount > 0 ? $"Retry failed ({FailedCount})" : "Retry failed";

    public string StorageSummary
    {
        get
        {
            var downloaded = _allItems.Count(x => x.IsDownloaded);
            var bytes = _allItems.Where(x => x.IsDownloaded).Sum(x => x.FileSizeBytes);
            return $"{downloaded} audio offline • {FormatBytes(bytes)}";
        }
    }

    public string FilterAllText => $"All ({_allItems.Count})";
    public string FilterDownloadedText => $"Downloaded ({_allItems.Count(x => x.IsDownloaded)})";
    public string FilterPendingText => $"Pending ({_allItems.Count(x => !x.IsDownloaded)})";

    public bool IsAllFilterActive => string.Equals(_activeFilter, "All", StringComparison.Ordinal);
    public bool IsDownloadedFilterActive => string.Equals(_activeFilter, "Downloaded", StringComparison.Ordinal);
    public bool IsPendingFilterActive => string.Equals(_activeFilter, "Pending", StringComparison.Ordinal);

    public ICommand BackCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand DownloadCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand PlayCommand { get; }
    public ICommand DownloadAllCommand { get; }
    public ICommand RetryFailedCommand { get; }
    public ICommand SetFilterCommand { get; }

    public MyAudioLibraryViewModel(IAudioLibraryService audioLibraryService, IAudioPlayerService audioPlayerService)
    {
        _audioLibraryService = audioLibraryService;
        _audioPlayerService = audioPlayerService;
        _audioLibraryService.LibraryChanged += OnLibraryChanged;
        _audioLibraryService.DownloadProgressChanged += OnDownloadProgressChanged;
        _audioPlayerService.PlaybackStateChanged += OnPlaybackStateChanged;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        RefreshCommand = new Command(async () => await RefreshAsync());
        DownloadCommand = new Command<AudioLibraryItem>(async item => await DownloadAsync(item));
        RemoveCommand = new Command<AudioLibraryItem>(async item => await RemoveAsync(item));
        PlayCommand = new Command<AudioLibraryItem>(async item => await PlayAsync(item));
        DownloadAllCommand = new Command(async () => await DownloadAllAsync());
        RetryFailedCommand = new Command(async () => await RetryFailedAsync());
        SetFilterCommand = new Command<string>(SetFilter);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        try
        {
            var lang = UserProfileService.PreferredLanguage;
            var items = await _audioLibraryService.GetLibraryItemsAsync(lang, cancellationToken);
            var failedCount = await _audioLibraryService.GetFailedCountAsync(lang, cancellationToken);
            var pendingCount = await _audioLibraryService.GetPendingQueueCountAsync(lang, cancellationToken);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _allItems.Clear();
                _allItems.AddRange(items);
                ApplyPlaybackState();
                FailedCount = failedCount;

                RebuildFilteredItems();
                StatusText = _allItems.Count == 0
                    ? "Chưa có dữ liệu tour. Hãy kết nối mạng để đồng bộ lần đầu."
                    : "Sẵn sàng tải audio offline";

                QueueEtaText = pendingCount > 0 ? $"Queue: {pendingCount} item(s) đang chờ" : "Queue idle";
                RaiseFilterHeadersChanged();
                OnPropertyChanged(nameof(StorageSummary));
            });
        }
        catch (Exception ex)
        {
            StatusText = $"Không thể tải thư viện: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DownloadAsync(AudioLibraryItem? item)
    {
        if (item is null || IsLoading)
        {
            return;
        }

        item.IsBusy = true;
        item.DownloadProgress = 0;
        item.DownloadStatusText = "Đang thêm vào queue...";
        RebuildFilteredItems();

        var queued = await _audioLibraryService.EnqueueDownloadsAsync([item.PoiId], item.LanguageCode);
        StatusText = queued > 0 ? $"Đã thêm vào hàng chờ: {item.Title}" : $"Mục đã tồn tại trong hàng chờ: {item.Title}";
    }

    private async Task DownloadAllAsync()
    {
        if (IsLoading)
        {
            return;
        }

        var pending = _allItems.Where(x => !x.IsDownloaded).Select(x => x.PoiId).ToList();
        if (pending.Count == 0)
        {
            StatusText = "Tất cả audio đã được tải offline.";
            return;
        }

        foreach (var item in _allItems.Where(x => !x.IsDownloaded))
        {
            item.IsBusy = true;
            item.DownloadProgress = 0;
            item.DownloadStatusText = "Đang chờ tải...";
        }

        RebuildFilteredItems();

        var queued = await _audioLibraryService.EnqueueDownloadsAsync(pending, UserProfileService.PreferredLanguage);
        StatusText = $"Đã thêm {queued} mục vào hàng chờ tải.";
    }

    private async Task RetryFailedAsync()
    {
        if (IsLoading)
        {
            return;
        }

        var retried = await _audioLibraryService.RetryFailedAsync(UserProfileService.PreferredLanguage);
        StatusText = retried > 0 ? $"Đã thêm lại {retried} mục lỗi vào queue." : "Không có mục lỗi để retry.";
    }

    private async Task RemoveAsync(AudioLibraryItem? item)
    {
        if (item is null || IsLoading)
        {
            return;
        }

        IsLoading = true;
        StatusText = $"Đang xóa: {item.Title}";

        var success = await _audioLibraryService.RemoveDownloadAsync(item.PoiId, item.LanguageCode);
        StatusText = success ? $"Đã xóa offline: {item.Title}" : $"Không thể xóa: {item.Title}";

        await RefreshAsync();
        IsLoading = false;
    }

    private async Task PlayAsync(AudioLibraryItem? item)
    {
        if (item is null || IsLoading)
        {
            return;
        }

        StatusText = $"Đang phát: {item.Title}";
        var started = await _audioLibraryService.PlayAsync(item.PoiId, item.LanguageCode);
        StatusText = started ? $"Đang phát offline: {item.Title}" : $"Không thể phát: {item.Title}";
        ApplyPlaybackState();
    }

    private void OnPlaybackStateChanged(object? sender, AudioPlaybackStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ApplyPlaybackState();

            if (e.IsPlaying)
            {
                var activeItem = _allItems.FirstOrDefault(x => IsSamePlayback(x, _audioPlayerService.CurrentPoiId, _audioPlayerService.CurrentLanguageCode));
                StatusText = activeItem is null ? "Đang phát audio" : $"Đang phát: {activeItem.Title}";
            }
            else if (StatusText.StartsWith("Đang phát", StringComparison.OrdinalIgnoreCase))
            {
                StatusText = "Sẵn sàng tải audio offline";
            }
        });
    }

    private void ApplyPlaybackState()
    {
        var currentPoiId = _audioPlayerService.CurrentPoiId;
        var currentLanguage = _audioPlayerService.CurrentLanguageCode;

        foreach (var item in _allItems)
        {
            item.IsPlaying = IsSamePlayback(item, currentPoiId, currentLanguage);
        }

        RebuildFilteredItems();
        RaiseFilterHeadersChanged();
        OnPropertyChanged(nameof(StorageSummary));
    }

    private static bool IsSamePlayback(AudioLibraryItem item, int? currentPoiId, string? currentLanguageCode)
    {
        if (!currentPoiId.HasValue || item.PoiId != currentPoiId.Value)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(currentLanguageCode))
        {
            return true;
        }

        return string.Equals(item.LanguageCode, currentLanguageCode, StringComparison.OrdinalIgnoreCase);
    }

    private void SetFilter(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return;
        }

        _activeFilter = filter;
        RebuildFilteredItems();
        RaiseFilterHeadersChanged();
    }

    private void OnDownloadProgressChanged(object? sender, AudioDownloadProgressChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (e.PoiId == 0)
            {
                if (!string.IsNullOrWhiteSpace(e.Message))
                {
                    StatusText = e.Message;
                }

                QueueEtaText = e.PendingQueueCount > 0
                    ? $"Queue: {e.PendingQueueCount} item(s) đang chờ"
                    : "Queue idle";

                return;
            }

            var item = _allItems.FirstOrDefault(x => x.PoiId == e.PoiId);
            if (item is null)
            {
                return;
            }

            item.IsBusy = !e.IsCompleted;
            item.DownloadProgress = e.Progress;
            item.DownloadStatusText = e.Message ?? (e.IsCompleted ? "Hoàn tất" : $"Đang tải {e.Progress * 100:F0}%");

            if (e.IsCompleted && !e.IsFailed)
            {
                item.IsDownloaded = item.DownloadProgress >= 1;
                item.FileSizeBytes = 0;
            }

            QueueEtaText = e.EstimatedRemaining is { } eta && e.PendingQueueCount > 0
                ? $"Queue: {e.PendingQueueCount} item(s), ETA ~ {FormatDuration(eta)}"
                : (e.PendingQueueCount > 0 ? $"Queue: {e.PendingQueueCount} item(s) đang chờ" : "Queue idle");

            RebuildFilteredItems();
            RaiseFilterHeadersChanged();

            FailedCount = await _audioLibraryService.GetFailedCountAsync(UserProfileService.PreferredLanguage);
        });
    }

    private void OnLibraryChanged(object? sender, EventArgs e)
    {
        _ = RefreshAsync();
    }

    private void RebuildFilteredItems()
    {
        IEnumerable<AudioLibraryItem> source = _allItems;

        source = _activeFilter switch
        {
            "Downloaded" => source.Where(x => x.IsDownloaded),
            "Pending" => source.Where(x => !x.IsDownloaded),
            _ => source
        };

        Items.Clear();
        foreach (var item in source)
        {
            Items.Add(item);
        }

        OnPropertyChanged(nameof(StorageSummary));
    }

    private void RaiseFilterHeadersChanged()
    {
        OnPropertyChanged(nameof(FilterAllText));
        OnPropertyChanged(nameof(FilterDownloadedText));
        OnPropertyChanged(nameof(FilterPendingText));
        OnPropertyChanged(nameof(IsAllFilterActive));
        OnPropertyChanged(nameof(IsDownloadedFilterActive));
        OnPropertyChanged(nameof(IsPendingFilterActive));
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        var kb = bytes / 1024d;
        if (kb < 1024)
        {
            return $"{kb:F1} KB";
        }

        var mb = kb / 1024d;
        if (mb < 1024)
        {
            return $"{mb:F1} MB";
        }

        var gb = mb / 1024d;
        return $"{gb:F2} GB";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 60)
        {
            return $"{Math.Max(1, (int)duration.TotalSeconds)}s";
        }

        if (duration.TotalMinutes < 60)
        {
            return $"{Math.Ceiling(duration.TotalMinutes)}m";
        }

        return $"{Math.Floor(duration.TotalHours)}h {duration.Minutes}m";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _audioLibraryService.LibraryChanged -= OnLibraryChanged;
        _audioLibraryService.DownloadProgressChanged -= OnDownloadProgressChanged;
        _audioPlayerService.PlaybackStateChanged -= OnPlaybackStateChanged;
    }
}

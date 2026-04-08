using TravelApp.Models.Runtime;

namespace TravelApp.Services.Abstractions;

public interface IAudioLibraryService
{
    event EventHandler? LibraryChanged;
    event EventHandler<AudioDownloadProgressChangedEventArgs>? DownloadProgressChanged;

    Task<IReadOnlyList<AudioLibraryItem>> GetLibraryItemsAsync(string? languageCode, CancellationToken cancellationToken = default);
    Task<bool> DownloadAsync(int poiId, string? languageCode, CancellationToken cancellationToken = default);
    Task<int> DownloadManyAsync(IEnumerable<int> poiIds, string? languageCode, CancellationToken cancellationToken = default);
    Task<int> EnqueueDownloadsAsync(IEnumerable<int> poiIds, string? languageCode, CancellationToken cancellationToken = default);
    Task<int> RetryFailedAsync(string? languageCode, CancellationToken cancellationToken = default);
    Task<bool> RemoveDownloadAsync(int poiId, string? languageCode, CancellationToken cancellationToken = default);
    Task<bool> PlayAsync(int poiId, string? languageCode, CancellationToken cancellationToken = default);
    Task<int> GetDownloadedCountAsync(string? languageCode, CancellationToken cancellationToken = default);
    Task<int> GetFailedCountAsync(string? languageCode, CancellationToken cancellationToken = default);
    Task<int> GetPendingQueueCountAsync(string? languageCode, CancellationToken cancellationToken = default);
}

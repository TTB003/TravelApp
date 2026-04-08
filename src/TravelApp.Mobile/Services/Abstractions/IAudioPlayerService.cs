using TravelApp.Models.Runtime;

namespace TravelApp.Services.Abstractions;

public interface IAudioPlayerService
{
    event EventHandler<AudioPlaybackStateChangedEventArgs>? PlaybackStateChanged;

    bool IsPlaying { get; }
    int? CurrentPoiId { get; }
    string? CurrentPoiTitle { get; }
    string? CurrentLanguageCode { get; }

    Task PlayAsync(AudioTriggerRequest request, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

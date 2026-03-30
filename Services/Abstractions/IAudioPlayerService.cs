using TravelApp.Models.Runtime;

namespace TravelApp.Services.Abstractions;

public interface IAudioPlayerService
{
    bool IsPlaying { get; }
    int? CurrentPoiId { get; }
    string? CurrentPoiTitle { get; }

    Task PlayAsync(AudioTriggerRequest request, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

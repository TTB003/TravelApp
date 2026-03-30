using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace TravelApp.Services.Runtime;

public class AutoAudioTriggerService : IAutoAudioTriggerService
{
    private static readonly TimeSpan TriggerCooldown = TimeSpan.FromMinutes(5);

    private readonly IGeofenceService _geofenceService;
    private readonly IAudioPlayerService _audioPlayerService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AutoAudioTriggerService> _logger;
    private readonly Dictionary<int, DateTimeOffset> _lastTriggerByPoi = [];
    private readonly object _sync = new();

    public event EventHandler<AudioTriggerRequest>? AudioTriggerRequested;

    public AutoAudioTriggerService(
        IGeofenceService geofenceService,
        IAudioPlayerService audioPlayerService,
        TimeProvider timeProvider,
        ILogger<AutoAudioTriggerService> logger)
    {
        _geofenceService = geofenceService;
        _audioPlayerService = audioPlayerService;
        _timeProvider = timeProvider;
        _logger = logger;

        _geofenceService.Entered += OnEntered;
    }

    private void OnEntered(object? sender, GeofenceTransitionEvent transitionEvent)
    {
        var now = _timeProvider.GetUtcNow();

        lock (_sync)
        {
            if (_lastTriggerByPoi.TryGetValue(transitionEvent.Poi.Id, out var lastTriggeredAt)
                && now - lastTriggeredAt < TriggerCooldown)
            {
                _logger.LogDebug("Audio trigger cooldown: skipped POI {PoiId} ({PoiTitle}).", transitionEvent.Poi.Id, transitionEvent.Poi.Title);
                return;
            }

            _lastTriggerByPoi[transitionEvent.Poi.Id] = now;
        }

        var languageCode = transitionEvent.Poi.PrimaryLanguage ?? "en";
        var request = new AudioTriggerRequest(transitionEvent.Poi, transitionEvent.UserLocation, languageCode, now);
        _logger.LogInformation("Audio trigger: POI {PoiId} ({PoiTitle}), language={LanguageCode}.", request.Poi.Id, request.Poi.Title, request.LanguageCode);
        AudioTriggerRequested?.Invoke(this, request);

        _ = TryPlayAsync(request);
    }

    private async Task TryPlayAsync(AudioTriggerRequest request)
    {
        try
        {
            if (_audioPlayerService.IsPlaying && _audioPlayerService.CurrentPoiId == request.Poi.Id)
            {
                _logger.LogDebug("Audio play ignored: same POI {PoiId} is already playing.", request.Poi.Id);
                return;
            }

            await _audioPlayerService.PlayAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audio trigger failed for POI {PoiId} ({PoiTitle}).", request.Poi.Id, request.Poi.Title);
        }
    }
}

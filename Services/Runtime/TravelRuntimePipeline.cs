using TravelApp.Models.Contracts;
using TravelApp.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace TravelApp.Services.Runtime;

public class TravelRuntimePipeline : ITravelRuntimePipeline
{
    private readonly ILocationTrackerService _locationTrackerService;
    private readonly IGeofenceService _geofenceService;
    private readonly IAutoAudioTriggerService _autoAudioTriggerService;
    private readonly IAudioPlayerService _audioPlayerService;
    private readonly ILogger<TravelRuntimePipeline> _logger;

    public TravelRuntimePipeline(
        ILocationTrackerService locationTrackerService,
        IGeofenceService geofenceService,
        IAutoAudioTriggerService autoAudioTriggerService,
        IAudioPlayerService audioPlayerService,
        ILogger<TravelRuntimePipeline> logger)
    {
        _locationTrackerService = locationTrackerService;
        _geofenceService = geofenceService;
        _autoAudioTriggerService = autoAudioTriggerService;
        _audioPlayerService = audioPlayerService;
        _logger = logger;
    }

    public async Task StartAsync(IEnumerable<PoiDto> pois, CancellationToken cancellationToken = default)
    {
        _geofenceService.SetPois(pois);

        _ = _autoAudioTriggerService;
        await _locationTrackerService.StartAsync(cancellationToken);
        _logger.LogInformation("Travel runtime pipeline started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _geofenceService.SetPois([]);
        await _locationTrackerService.StopAsync(cancellationToken);
        await _audioPlayerService.StopAsync(cancellationToken);
        _logger.LogInformation("Travel runtime pipeline stopped.");
    }
}

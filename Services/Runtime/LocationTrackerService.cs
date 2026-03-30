using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace TravelApp.Services.Runtime;

public class LocationTrackerService : ILocationTrackerService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const double MinDistanceForUpdateMeters = 5;

    private readonly ILocationProvider _locationProvider;
    private readonly ILogService _logService;
    private readonly ILogger<LocationTrackerService> _logger;
    private CancellationTokenSource? _trackingCts;
    private Task? _trackingTask;

    public event EventHandler<LocationSample>? LocationChanged;

    public LocationSample? CurrentLocation { get; private set; }

    public LocationTrackerService(ILocationProvider locationProvider, ILogService logService, ILogger<LocationTrackerService> logger)
    {
        _locationProvider = locationProvider;
        _logService = logService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_trackingTask is { IsCompleted: false })
        {
            _logger.LogDebug("GPS tracker: start ignored because tracker is already running.");
            return Task.CompletedTask;
        }

        _trackingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _trackingTask = TrackLoopAsync(_trackingCts.Token);
        _logger.LogInformation("GPS tracker: started with {PollIntervalSeconds}s polling interval.", PollInterval.TotalSeconds);
        _logService.Log("GPS", $"Tracker started (interval={PollInterval.TotalSeconds:0}s)");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_trackingCts is null || _trackingTask is null)
        {
            return;
        }

        _trackingCts.Cancel();

        try
        {
            await _trackingTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _trackingCts.Dispose();
            _trackingCts = null;
            _trackingTask = null;
            _logger.LogInformation("GPS tracker: stopped.");
            _logService.Log("GPS", "Tracker stopped");
        }
    }

    private async Task TrackLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (!cancellationToken.IsCancellationRequested)
        {
            var sample = await _locationProvider.GetCurrentLocationAsync(cancellationToken);
            if (sample is not null)
            {
                if (CurrentLocation is not null)
                {
                    var distance = CalculateDistanceMeters(CurrentLocation.Latitude, CurrentLocation.Longitude, sample.Latitude, sample.Longitude);
                    if (distance < MinDistanceForUpdateMeters)
                    {
                        _logger.LogDebug("GPS update skipped: movement {DistanceMeters:F1}m < threshold {ThresholdMeters:F1}m.", distance, MinDistanceForUpdateMeters);
                        goto wait_for_next_tick;
                    }
                }

                CurrentLocation = sample;
                LocationChanged?.Invoke(this, sample);
                _logger.LogInformation("GPS update: lat={Latitude:F6}, lng={Longitude:F6}, at={TimestampUtc:O}", sample.Latitude, sample.Longitude, sample.TimestampUtc);
                _logService.Log("GPS", $"Update lat={sample.Latitude:F6}, lng={sample.Longitude:F6}");
            }

wait_for_next_tick:
            if (!await timer.WaitForNextTickAsync(cancellationToken))
            {
                break;
            }
        }
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000;
        static double ToRadians(double value) => value * Math.PI / 180;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }
}

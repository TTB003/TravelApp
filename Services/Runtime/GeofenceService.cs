using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace TravelApp.Services.Runtime;

public class GeofenceService : IGeofenceService
{
    private static readonly TimeSpan EnterDebounce = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan EnterCooldown = TimeSpan.FromMinutes(5);
    private const double DefaultRadiusMeters = 100;

    private readonly ILocationTrackerService _locationTrackerService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogService _logService;
    private readonly ILogger<GeofenceService> _logger;
    private readonly object _sync = new();

    private IReadOnlyList<PoiDto> _pois = [];
    private readonly Dictionary<int, GeofencePoiState> _states = [];
    private LocationSample? _latestLocation;

    public event EventHandler<GeofenceTransitionEvent>? TransitionOccurred;
    public event EventHandler<GeofenceTransitionEvent>? Entered;
    public event EventHandler<GeofenceTransitionEvent>? Exited;

    public GeofenceService(ILocationTrackerService locationTrackerService, TimeProvider timeProvider, ILogService logService, ILogger<GeofenceService> logger)
    {
        _locationTrackerService = locationTrackerService;
        _timeProvider = timeProvider;
        _logService = logService;
        _logger = logger;

        _locationTrackerService.LocationChanged += OnLocationChanged;
    }

    public void SetPois(IEnumerable<PoiDto> pois)
    {
        var snapshot = pois?.ToList() ?? [];

        lock (_sync)
        {
            _pois = snapshot;
            _logger.LogInformation("Geofence: loaded {PoiCount} POIs for monitoring.", _pois.Count);

            var validPoiIds = snapshot.Select(x => x.Id).ToHashSet();
            var staleKeys = _states.Keys.Where(x => !validPoiIds.Contains(x)).ToList();
            foreach (var stale in staleKeys)
            {
                CancelPendingEnter(_states[stale]);
                _states.Remove(stale);
            }

            foreach (var poi in snapshot)
            {
                if (!_states.ContainsKey(poi.Id))
                {
                    _states[poi.Id] = new GeofencePoiState();
                }
            }
        }
    }

    private void OnLocationChanged(object? sender, LocationSample location)
    {
        List<(PoiDto Poi, GeofenceTransitionEvent Event)> transitionsToRaise = [];

        lock (_sync)
        {
            _latestLocation = location;

            foreach (var poi in _pois)
            {
                var state = GetState(poi.Id);
                var radius = poi.GeofenceRadiusMeters ?? DefaultRadiusMeters;
                var distance = CalculateDistanceMeters(location.Latitude, location.Longitude, poi.Latitude, poi.Longitude);
                var inside = distance <= radius;

                if (inside)
                {
                    if (state.IsInside || state.PendingEnter is not null)
                    {
                        continue;
                    }

                    if (IsInCooldown(state))
                    {
                        state.IsInside = true;
                        var remaining = EnterCooldown - (_timeProvider.GetUtcNow() - state.LastEnterAtUtc!.Value);
                        _logger.LogDebug("Geofence cooldown: POI {PoiId} ({PoiTitle}) ignored, remaining={RemainingSeconds:F0}s.", poi.Id, poi.Title, Math.Max(0, remaining.TotalSeconds));
                        continue;
                    }

                    state.PendingEnter = ScheduleEnterDebounce(poi, state);
                    continue;
                }

                CancelPendingEnter(state);

                if (!state.IsInside)
                {
                    continue;
                }

                state.IsInside = false;
                var exitEvent = new GeofenceTransitionEvent(
                    poi,
                    GeofenceTransitionType.Exit,
                    location,
                    distance,
                    _timeProvider.GetUtcNow());

                transitionsToRaise.Add((poi, exitEvent));
                _logger.LogInformation("Geofence EXIT: POI {PoiId} ({PoiTitle}) at distance {DistanceMeters:F1}m.", poi.Id, poi.Title, distance);
                _logService.Log("Geofence", $"EXIT poi={poi.Id} ({poi.Title}) distance={distance:F1}m");
            }
        }

        foreach (var (_, transitionEvent) in transitionsToRaise)
        {
            TransitionOccurred?.Invoke(this, transitionEvent);
            Exited?.Invoke(this, transitionEvent);
        }
    }

    private CancellationTokenSource ScheduleEnterDebounce(PoiDto poi, GeofencePoiState state)
    {
        var cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogDebug("Geofence debounce: scheduling ENTER evaluation for POI {PoiId} ({PoiTitle}) in {DebounceSeconds}s.", poi.Id, poi.Title, EnterDebounce.TotalSeconds);
                await Task.Delay(EnterDebounce, cts.Token);
                TryCommitEnter(poi, cts);
            }
            catch (OperationCanceledException)
            {
            }
        }, cts.Token);

        return cts;
    }

    private void TryCommitEnter(PoiDto poi, CancellationTokenSource pendingCts)
    {
        GeofenceTransitionEvent? transitionEvent = null;

        lock (_sync)
        {
            var state = GetState(poi.Id);
            if (!ReferenceEquals(state.PendingEnter, pendingCts))
            {
                return;
            }

            state.PendingEnter = null;
            pendingCts.Dispose();

            if (_latestLocation is null)
            {
                return;
            }

            var radius = poi.GeofenceRadiusMeters ?? DefaultRadiusMeters;
            var distance = CalculateDistanceMeters(_latestLocation.Latitude, _latestLocation.Longitude, poi.Latitude, poi.Longitude);
            var stillInside = distance <= radius;
            if (!stillInside)
            {
                return;
            }

            if (IsInCooldown(state))
            {
                state.IsInside = true;
                return;
            }

            state.IsInside = true;
            state.LastEnterAtUtc = _timeProvider.GetUtcNow();
            transitionEvent = new GeofenceTransitionEvent(
                poi,
                GeofenceTransitionType.Enter,
                _latestLocation,
                distance,
                state.LastEnterAtUtc.Value);

            _logger.LogInformation("Geofence ENTER: POI {PoiId} ({PoiTitle}) at distance {DistanceMeters:F1}m.", poi.Id, poi.Title, distance);
            _logService.Log("Geofence", $"ENTER poi={poi.Id} ({poi.Title}) distance={distance:F1}m");
        }

        if (transitionEvent is null)
        {
            return;
        }

        TransitionOccurred?.Invoke(this, transitionEvent);
        Entered?.Invoke(this, transitionEvent);
    }

    private GeofencePoiState GetState(int poiId)
    {
        if (_states.TryGetValue(poiId, out var state))
        {
            return state;
        }

        state = new GeofencePoiState();
        _states[poiId] = state;
        return state;
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

    private static void CancelPendingEnter(GeofencePoiState state)
    {
        if (state.PendingEnter is null)
        {
            return;
        }

        state.PendingEnter.Cancel();
        state.PendingEnter.Dispose();
        state.PendingEnter = null;
    }

    private bool IsInCooldown(GeofencePoiState state)
    {
        if (!state.LastEnterAtUtc.HasValue)
        {
            return false;
        }

        var elapsed = _timeProvider.GetUtcNow() - state.LastEnterAtUtc.Value;
        return elapsed < EnterCooldown;
    }

    private sealed class GeofencePoiState
    {
        public bool IsInside { get; set; }
        public DateTimeOffset? LastEnterAtUtc { get; set; }
        public CancellationTokenSource? PendingEnter { get; set; }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using TravelApp.Models.Runtime;
using TravelApp.Services;
using TravelApp.Services.Abstractions;
using TravelApp.ViewModels;

namespace TravelApp;

public partial class TourMapRoutePage : ContentPage, IQueryAttributable
{
    private static readonly Color[] SegmentColors =
    [
        Color.FromArgb("#E31667"),
        Color.FromArgb("#2D9CDB"),
        Color.FromArgb("#27AE60"),
        Color.FromArgb("#F2994A"),
        Color.FromArgb("#9B51E0")
    ];

    private const double PulseMinMeters = 26;
    private const double PulseMaxMeters = 86;
    private const double PulseStepMeters = 4;

    private readonly TourMapRouteViewModel _viewModel;
    private readonly ITourRouteGeometryService _routeGeometryService;
    private readonly Microsoft.Maui.Controls.Maps.Map _map;
    private int? _tourId;
    private int? _poiId;
    private bool _routeLoaded;
    private bool _isLoadingRoute;
    private IDispatcherTimer? _pulseTimer;
    private Circle? _activePulseCircle;
    private double _pulseRadiusMeters = PulseMinMeters;
    private bool _pulseExpanding = true;
    private RouteGeometryResult _routeGeometry = new();

    public TourMapRoutePage()
    {
        InitializeComponent();
        _viewModel = MauiProgram.Services.GetRequiredService<TourMapRouteViewModel>();
        _routeGeometryService = MauiProgram.Services.GetRequiredService<ITourRouteGeometryService>();
        BindingContext = _viewModel;
        _viewModel.RouteChanged += OnRouteChanged;

        _map = new Microsoft.Maui.Controls.Maps.Map
        {
            MapType = MapType.Street,
            IsTrafficEnabled = false,
            IsShowingUser = false
        };

        TourMapContainer.Children.Add(_map);
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("tourId", out var tourIdValue))
        {
            return;
        }

        if (query.TryGetValue("poiId", out var poiIdValue) && int.TryParse(poiIdValue?.ToString(), out var poiId))
        {
            _poiId = poiId;
        }

        if (int.TryParse(tourIdValue?.ToString(), out var tourId))
        {
            _tourId = tourId;
            await LoadRouteAsync();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        StartPulseTimer();

        if (_tourId.HasValue && !_routeLoaded)
        {
            await LoadRouteAsync();
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        StopPulseTimer();
        await _viewModel.StopAsync();
        _viewModel.RouteChanged -= OnRouteChanged;
        _viewModel.Dispose();
    }

    private async Task LoadRouteAsync()
    {
        if (!_tourId.HasValue || _isLoadingRoute || _routeLoaded)
        {
            return;
        }

        _isLoadingRoute = true;
        try
        {
            await _viewModel.LoadAsync(_tourId.Value, _poiId);
            _routeGeometry = _viewModel.Tour is null
                ? new RouteGeometryResult()
                : await _routeGeometryService.GetRoadPathAsync(_viewModel.Tour, UserProfileService.PreferredLanguage);
            _routeLoaded = true;
            RenderRoute();
        }
        finally
        {
            _isLoadingRoute = false;
        }
    }

    private void OnRouteChanged(object? sender, EventArgs e)
    {
        RenderRoute();
    }

    private void RenderRoute()
    {
        _map.Pins.Clear();
        _map.MapElements.Clear();

        var waypoints = _viewModel.Waypoints;
        if (waypoints.Count == 0)
        {
            return;
        }

        var selectedWaypoint = _viewModel.SelectedWaypoint ?? waypoints.First();

        MoveMapToSelectedWaypoint(selectedWaypoint, waypoints.Count);
        RenderRouteSegments();
        RenderWaypoints(waypoints, selectedWaypoint);
        ConfigurePulseCircle(selectedWaypoint);
    }

    private void RenderRouteSegments()
    {
        var segments = _routeGeometry.Segments.Count > 0
            ? _routeGeometry.Segments
            : new[]
            {
                new RouteGeometrySegment
                {
                    Index = 0,
                    Label = "Route",
                    Points = _viewModel.Waypoints.Select(x => new Location(x.Latitude, x.Longitude)).ToList()
                }
            };

        foreach (var segment in segments)
        {
            if (segment.Points.Count < 2)
            {
                continue;
            }

            var color = SegmentColors[segment.Index % SegmentColors.Length];
            var polyline = new Polyline
            {
                StrokeColor = color,
                StrokeWidth = 6
            };

            foreach (var location in segment.Points)
            {
                polyline.Geopath.Add(location);
            }

            _map.MapElements.Add(polyline);
        }
    }

    private void RenderWaypoints(IReadOnlyList<TourMapWaypoint> waypoints, TourMapWaypoint selectedWaypoint)
    {
        foreach (var waypoint in waypoints)
        {
            var location = new Location(waypoint.Latitude, waypoint.Longitude);
            var isActive = selectedWaypoint.PoiId == waypoint.PoiId;
            var pin = new Pin
            {
                Label = isActive ? $"▶ {waypoint.SortOrder}. {waypoint.Title}" : $"{waypoint.SortOrder}. {waypoint.Title}",
                Address = waypoint.Location,
                Type = isActive ? PinType.SavedPin : PinType.Place,
                Location = location
            };

            pin.InfoWindowClicked += (_, _) =>
            {
                if (_viewModel.SelectWaypointCommand is Command command)
                {
                    command.Execute(waypoint);
                }
            };

            _map.Pins.Add(pin);
        }
    }

    private void MoveMapToSelectedWaypoint(TourMapWaypoint selectedWaypoint, int waypointCount)
    {
        var zoomMeters = waypointCount <= 1 ? 700d : 550d;
        var center = new Location(selectedWaypoint.Latitude, selectedWaypoint.Longitude);
        _map.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromMeters(zoomMeters)));
    }

    private void ConfigurePulseCircle(TourMapWaypoint selectedWaypoint)
    {
        if (_activePulseCircle is not null)
        {
            _map.MapElements.Remove(_activePulseCircle);
            _activePulseCircle = null;
        }

        var location = new Location(selectedWaypoint.Latitude, selectedWaypoint.Longitude);
        _activePulseCircle = new Circle
        {
            Center = location,
            Radius = Distance.FromMeters(PulseMinMeters),
            StrokeColor = Color.FromArgb("#E31667"),
            StrokeWidth = 2,
            FillColor = Color.FromArgb("#22E31667")
        };

        _map.MapElements.Add(_activePulseCircle);
        _pulseRadiusMeters = PulseMinMeters;
        _pulseExpanding = true;
    }

    private void StartPulseTimer()
    {
        if (_pulseTimer is not null)
        {
            _pulseTimer.Start();
            return;
        }

        _pulseTimer = Dispatcher.CreateTimer();
        _pulseTimer.Interval = TimeSpan.FromMilliseconds(220);
        _pulseTimer.Tick += OnPulseTimerTick;
        _pulseTimer.Start();
    }

    private void StopPulseTimer()
    {
        if (_pulseTimer is null)
        {
            return;
        }

        _pulseTimer.Stop();
        _pulseTimer.Tick -= OnPulseTimerTick;
        _pulseTimer = null;
    }

    private void OnPulseTimerTick(object? sender, EventArgs e)
    {
        if (_activePulseCircle is null)
        {
            return;
        }

        if (_pulseExpanding)
        {
            _pulseRadiusMeters += PulseStepMeters;
            if (_pulseRadiusMeters >= PulseMaxMeters)
            {
                _pulseRadiusMeters = PulseMaxMeters;
                _pulseExpanding = false;
            }
        }
        else
        {
            _pulseRadiusMeters -= PulseStepMeters;
            if (_pulseRadiusMeters <= PulseMinMeters)
            {
                _pulseRadiusMeters = PulseMinMeters;
                _pulseExpanding = true;
            }
        }

        _activePulseCircle.Radius = Distance.FromMeters(_pulseRadiusMeters);
    }
}

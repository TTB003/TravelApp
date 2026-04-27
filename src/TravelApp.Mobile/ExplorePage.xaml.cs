using System.Collections.Specialized;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TravelApp.Models;
using TravelApp.Mobile.Services;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;
using TravelApp.ViewModels;

namespace TravelApp;

public partial class ExplorePage : ContentPage
{
    private readonly ExploreViewModel _viewModel;
    private readonly ITravelBootstrapService _travelBootstrapService;
    private readonly IAudioPlayerService _audioPlayerService;
    private readonly ILocationPollingService _locationPollingService;
    private readonly ILogger<ExplorePage> _logger;
    private Microsoft.Maui.Controls.Maps.Map? _map;
    private Microsoft.Maui.Controls.Maps.Map? _discoverMap;
    private IDispatcherTimer? _audioStatusTimer;
    private bool _explorePinsAttached;
    private bool _isRenderingPins;
    private int? _selectedExplorePoiId;
    private LocationSample? _currentLocation;

    public ExplorePage()
    {
        InitializeComponent();
        _viewModel = MauiProgram.Services.GetRequiredService<ExploreViewModel>();
        BindingContext = _viewModel;
        _travelBootstrapService = MauiProgram.Services.GetRequiredService<ITravelBootstrapService>();
        _audioPlayerService = MauiProgram.Services.GetRequiredService<IAudioPlayerService>();
        _locationPollingService = MauiProgram.Services.GetRequiredService<ILocationPollingService>();
        _logger = MauiProgram.Services.GetRequiredService<ILogger<ExplorePage>>();
        AttachExplorePinsObservers();
        _locationPollingService.OnLocationUpdated += OnLocationUpdated;
        InitializeMap();
        InitializeDiscoverMap();
        UpdateAudioStatus();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.ResetBottomTabToExplore();
        StartAudioStatusTimer();
        _ = _viewModel.RefreshAsync();
        _ = StartRuntimeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopAudioStatusTimer();
        DetachExplorePinsObservers();
        _locationPollingService.OnLocationUpdated -= OnLocationUpdated;
        _ = StopRuntimeAsync();
    }

    private void InitializeMap()
    {
#if WINDOWS
        if (string.IsNullOrWhiteSpace(Windows.Services.Maps.MapService.ServiceToken))
        {
            _logger.LogWarning("Windows map token is missing. Set BING_MAPS_KEY to show map on Windows.");
            var placeholderContainer = this.FindByName<Grid>("ExploreMapContainer");
            if (placeholderContainer is not null)
            {
                placeholderContainer.Children.Clear();
                placeholderContainer.Children.Add(new Label
                {
                    Text = "Map needs BING_MAPS_KEY on Windows.",
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    TextColor = Colors.Gray,
                    Margin = new Thickness(12)
                });
            }

            return;
        }
#endif

        InitializeMapInContainer("ExploreMapContainer", showUser: true, radiusKm: 1);
    }

    private void InitializeDiscoverMap()
    {
#if WINDOWS
        if (string.IsNullOrWhiteSpace(Windows.Services.Maps.MapService.ServiceToken))
        {
            return;
        }
#endif

        InitializeMapInContainer("DiscoverMapContainer", showUser: true, radiusKm: 1.2);
    }

    private void InitializeMapInContainer(string containerName, bool showUser, double radiusKm)
    {
        var map = new Microsoft.Maui.Controls.Maps.Map
        {
            MapType = MapType.Street,
            IsTrafficEnabled = false,
            IsShowingUser = showUser
        };

        var mapContainer = this.FindByName<Grid>(containerName);
        if (mapContainer is null)
        {
            _logger.LogWarning("Map container '{ContainerName}' was not found in XAML.", containerName);
            return;
        }

        mapContainer.Children.Clear();
        mapContainer.Children.Add(map);

        if (string.Equals(containerName, "ExploreMapContainer", StringComparison.OrdinalIgnoreCase))
        {
            _map = map;
        }
        else if (string.Equals(containerName, "DiscoverMapContainer", StringComparison.OrdinalIgnoreCase))
        {
            _discoverMap = map;
        }

        var hoChiMinhCity = new Location(10.7769, 106.7009);
        map.MoveToRegion(MapSpan.FromCenterAndRadius(hoChiMinhCity, Distance.FromKilometers(radiusKm)));

        if (string.Equals(containerName, "ExploreMapContainer", StringComparison.OrdinalIgnoreCase))
        {
            RenderExplorePins();
        }
    }

    private void AttachExplorePinsObservers()
    {
        if (_explorePinsAttached)
        {
            return;
        }

        _viewModel.ForYouItems.CollectionChanged += OnExplorePoisChanged;
        _viewModel.EditorsChoiceItems.CollectionChanged += OnExplorePoisChanged;
        _explorePinsAttached = true;
    }

    private void DetachExplorePinsObservers()
    {
        if (!_explorePinsAttached)
        {
            return;
        }

        _viewModel.ForYouItems.CollectionChanged -= OnExplorePoisChanged;
        _viewModel.EditorsChoiceItems.CollectionChanged -= OnExplorePoisChanged;
        _explorePinsAttached = false;
    }

    private void OnExplorePoisChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isRenderingPins) return;
        _isRenderingPins = true;

        // Đợi 200ms để đợi các thay đổi khác trong cùng batch rồi mới vẽ lại map
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () =>
        {
            RenderExplorePins();
            _isRenderingPins = false;
        });
    }

    private void RenderExplorePins()
    {
        if (_map is null)
        {
            return;
        }

        var pois = _viewModel.ForYouItems
            .Concat(_viewModel.EditorsChoiceItems)
            .DistinctBy(x => x.Id)
            .Where(HasValidCoordinates)
            .ToList();
        var userLocation = _currentLocation;
        var points = new List<Location>();

        if (pois.Count == 0 && userLocation is null)
        {
            return;
        }

        _map.Pins.Clear();

        if (userLocation is not null)
        {
            var location = new Location(userLocation.Latitude, userLocation.Longitude);
            points.Add(location);

            _map.Pins.Add(new Pin
            {
                Label = "📍 Vị trí của bạn",
                Address = "Your current location",
                Location = location,
                Type = PinType.Place
            });
        }

        foreach (var poi in pois)
        {
            var pin = new Pin
            {
                Label = _selectedExplorePoiId == poi.Id ? $"★ {poi.Title}" : poi.Title,
                Address = poi.Location,
                Type = _selectedExplorePoiId == poi.Id ? PinType.SavedPin : PinType.Place,
                Location = new Location(poi.Latitude, poi.Longitude)
            };

            points.Add(pin.Location);

            pin.MarkerClicked += async (_, args) =>
            {
                _selectedExplorePoiId = poi.Id;
                RenderExplorePins();
                args.HideInfoWindow = true;
                await AnimateToPoiAsync(poi.Latitude, poi.Longitude);
                await Task.Delay(120);

                if (_viewModel.OpenTourDetailCommand is Command<PoiModel> command)
                {
                    command.Execute(poi);
                }
            };

            _map.Pins.Add(pin);
        }

        MoveMapToFitPoints(points);
    }

    private void MoveMapToFitPoints(IReadOnlyList<Location> points)
    {
        if (_map is null || points.Count == 0)
        {
            return;
        }

        var minLat = points.Min(x => x.Latitude);
        var maxLat = points.Max(x => x.Latitude);
        var minLng = points.Min(x => x.Longitude);
        var maxLng = points.Max(x => x.Longitude);

        var center = new Location((minLat + maxLat) / 2d, (minLng + maxLng) / 2d);
        var latSpanKm = Math.Max(0.8, (maxLat - minLat) * 111.32 * 1.35);
        var lngSpanKm = Math.Max(0.8, (maxLng - minLng) * 111.32 * 1.35);
        var radiusKm = Math.Max(latSpanKm, lngSpanKm) / 2d;

        _map.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(Math.Min(5d, Math.Max(0.9d, radiusKm)))));
    }

    private void OnLocationUpdated(LocationSample sample)
    {
        // Chỉ cập nhật nếu vị trí mới cách vị trí cũ trên 10m để tránh lag
        if (_currentLocation != null)
        {
            var distance = Location.CalculateDistance(
                _currentLocation.Latitude, _currentLocation.Longitude,
                sample.Latitude, sample.Longitude, DistanceUnits.Kilometers) * 1000;

            if (distance < 10) return; 
        }

        _currentLocation = sample;
        _ = MainThread.InvokeOnMainThreadAsync(RenderExplorePins);
    }

    private Task AnimateToPoiAsync(double latitude, double longitude)
    {
        if (_map is null)
        {
            return Task.CompletedTask;
        }

        _map.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(latitude, longitude), Distance.FromMeters(420)));
        return Task.CompletedTask;
    }

    private static bool HasValidCoordinates(PoiModel poi)
    {
        return poi.Latitude != 0d || poi.Longitude != 0d;
    }

    private async Task StartRuntimeAsync()
    {
        try
        {
            await _travelBootstrapService.StartAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Explore page: failed to start runtime pipeline.");
        }
    }

    private async Task StopRuntimeAsync()
    {
        try
        {
            await _travelBootstrapService.StopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Explore page: failed to stop runtime pipeline.");
        }
    }

    private void StartAudioStatusTimer()
    {
        if (_audioStatusTimer is not null)
        {
            _audioStatusTimer.Start();
            return;
        }

        _audioStatusTimer = Dispatcher.CreateTimer();
        _audioStatusTimer.Interval = TimeSpan.FromMilliseconds(500);
        _audioStatusTimer.Tick += OnAudioStatusTick;
        _audioStatusTimer.Start();
    }

    private void StopAudioStatusTimer()
    {
        if (_audioStatusTimer is null)
        {
            return;
        }

        _audioStatusTimer.Stop();
        _audioStatusTimer.Tick -= OnAudioStatusTick;
        _audioStatusTimer = null;

        AudioStatusBorder.IsVisible = false;
    }

    private void OnAudioStatusTick(object? sender, EventArgs e)
    {
        UpdateAudioStatus();
    }

    private void UpdateAudioStatus()
    {
        var isPlaying = _audioPlayerService.IsPlaying;
        AudioStatusBorder.IsVisible = isPlaying;

        if (!isPlaying)
        {
            AudioStatusTextLabel.Text = "";
            AudioPoiTitleLabel.Text = "";
            return;
        }

        AudioStatusTextLabel.Text = LocalizationManager.Instance.NowPlayingAudioText;
        AudioPoiTitleLabel.Text = _audioPlayerService.CurrentPoiTitle ?? "Địa điểm hiện tại";
    }
}

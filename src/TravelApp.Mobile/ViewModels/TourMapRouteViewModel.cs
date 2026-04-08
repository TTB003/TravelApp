using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Models.Runtime;
using TravelApp.Models.Contracts;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public sealed class TourMapRouteViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ITourRouteCatalogService _tourRouteCatalogService;
    private readonly ITourRoutePlaybackService _tourRoutePlaybackService;
    private string _statusText = "Đang lấy route...";
    private int? _selectedPoiId;
    private int? _anchorPoiId;
    private bool _isLoading;

    public ObservableCollection<TourMapWaypoint> Waypoints { get; } = [];

    public TourMapWaypoint? SelectedWaypoint { get; private set; }
    public TourRouteDto? Tour { get; private set; }
    public bool HasActiveWaypoint => SelectedWaypoint is not null;
    public string CurrentWaypointTitle => SelectedWaypoint?.Title ?? "Chưa có điểm đang phát";
    public string CurrentWaypointSubtitle => SelectedWaypoint?.Location ?? "Hãy di chuyển đến điểm đầu tiên để bắt đầu";
    public string CurrentWaypointProgressText => Waypoints.Count == 0 || SelectedWaypoint is null
        ? "0/0"
        : $"{SelectedWaypoint.SortOrder}/{Waypoints.Count}";
    public string CurrentWaypointDistanceText => SelectedWaypoint?.DistanceMeters is null
        ? string.Empty
        : $"Khoảng cách chặng trước: {SelectedWaypoint.DistanceMeters:F0} m";
    public double CurrentWaypointProgressValue => Waypoints.Count == 0 || SelectedWaypoint is null
        ? 0
        : (double)SelectedWaypoint.SortOrder / Waypoints.Count;

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

    public ICommand BackCommand { get; }
    public ICommand OpenPoiCommand { get; }
    public ICommand SelectWaypointCommand { get; }
    public ICommand RecenterCommand { get; }

    public event EventHandler? RouteChanged;

    public TourMapRouteViewModel(ITourRouteCatalogService tourRouteCatalogService, ITourRoutePlaybackService tourRoutePlaybackService)
    {
        _tourRouteCatalogService = tourRouteCatalogService;
        _tourRoutePlaybackService = tourRoutePlaybackService;
        _tourRoutePlaybackService.ActiveWaypointChanged += OnActiveWaypointChanged;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        SelectWaypointCommand = new Command<TourMapWaypoint>(async waypoint => await SelectWaypointAsync(waypoint));
        OpenPoiCommand = new Command<TourMapWaypoint>(async waypoint =>
        {
            if (waypoint is null)
            {
                return;
            }

            await SelectWaypointAsync(waypoint);

            await Shell.Current.GoToAsync($"TourDetailPage?tourId={waypoint.PoiId}");
        });
        RecenterCommand = new Command(() => RouteChanged?.Invoke(this, EventArgs.Empty));
    }

    public async Task LoadAsync(int anchorPoiId, int? preferredPoiId = null, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        if (_anchorPoiId == anchorPoiId && Waypoints.Count > 0)
        {
            if (preferredPoiId.HasValue)
            {
                var existingWaypoint = Waypoints.FirstOrDefault(x => x.PoiId == preferredPoiId.Value);
                if (existingWaypoint is not null && SelectedWaypoint?.PoiId != existingWaypoint.PoiId)
                {
                    await _tourRoutePlaybackService.SelectWaypointAsync(existingWaypoint.PoiId, cancellationToken);
                }
            }

            return;
        }

        IsLoading = true;
        StatusText = "Đang tải tour...";

        try
        {
            var route = await _tourRouteCatalogService.GetRouteAsync(anchorPoiId, languageCode ?? UserProfileService.PreferredLanguage, cancellationToken);
            if (route is null)
            {
                Tour = null;
                Waypoints.Clear();
                SelectedWaypoint = null;
                _selectedPoiId = null;
                _anchorPoiId = null;
                StatusText = "Không tìm thấy dữ liệu route của tour.";
                OnPropertyChanged(nameof(Tour));
                RouteChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            Tour = route;
            _anchorPoiId = anchorPoiId;

            Waypoints.Clear();
            foreach (var (waypoint, index) in route.Waypoints.Select((x, i) => (x, i)))
            {
                Waypoints.Add(new TourMapWaypoint
                {
                    PoiId = waypoint.Poi.Id,
                    SortOrder = waypoint.SortOrder == 0 ? index + 1 : waypoint.SortOrder,
                    Title = waypoint.Poi.Title,
                    Location = waypoint.Poi.Location,
                    Latitude = waypoint.Poi.Latitude,
                    Longitude = waypoint.Poi.Longitude,
                    DistanceMeters = waypoint.DistanceFromPreviousMeters,
                    Poi = waypoint.Poi
                });
            }

            var preferredWaypoint = preferredPoiId.HasValue
                ? Waypoints.FirstOrDefault(x => x.PoiId == preferredPoiId.Value)
                : null;
            SetSelectedWaypoint(preferredWaypoint ?? Waypoints.FirstOrDefault(), raiseRouteChanged: false);
            StatusText = Waypoints.Count == 0
                ? "Tour chưa có waypoint nào."
                : $"{Waypoints.Count} điểm dừng • {route.TotalDistanceMeters / 1000d:0.0} km";

            await _tourRoutePlaybackService.StartAsync(route, preferredPoiId, cancellationToken);
            OnPropertyChanged(nameof(Tour));
            RouteChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Tour = null;
            Waypoints.Clear();
            SelectedWaypoint = null;
            _selectedPoiId = null;
            _anchorPoiId = null;
            StatusText = $"Lỗi tải tour: {ex.Message}";
            OnPropertyChanged(nameof(Tour));
            RouteChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _tourRoutePlaybackService.StopAsync(cancellationToken);
    }

    private async Task SelectWaypointAsync(TourMapWaypoint? waypoint)
    {
        if (waypoint is not null)
        {
            SetSelectedWaypoint(waypoint, raiseRouteChanged: true);
            await _tourRoutePlaybackService.SelectWaypointAsync(waypoint.PoiId);
        }
    }

    private void OnActiveWaypointChanged(object? sender, TourRoutePlaybackChangedEventArgs e)
    {
        var waypoint = e.Waypoint is null
            ? null
            : Waypoints.FirstOrDefault(x => x.PoiId == e.Waypoint.Poi.Id);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            SetSelectedWaypoint(waypoint, raiseRouteChanged: true);
            if (e.UserLocation is not null && waypoint is not null)
            {
                StatusText = $"Đang ở điểm {waypoint.SortOrder} • {waypoint.Title}";
            }
        });
    }

    private void SetSelectedWaypoint(TourMapWaypoint? waypoint, bool raiseRouteChanged)
    {
        _selectedPoiId = waypoint?.PoiId;

        var refreshed = Waypoints.Select(item => new TourMapWaypoint
        {
            PoiId = item.PoiId,
            SortOrder = item.SortOrder,
            Title = item.Title,
            Location = item.Location,
            Latitude = item.Latitude,
            Longitude = item.Longitude,
            DistanceMeters = item.DistanceMeters,
            Poi = item.Poi,
            IsActive = item.PoiId == _selectedPoiId
        }).ToList();

        Waypoints.Clear();
        foreach (var item in refreshed)
        {
            Waypoints.Add(item);
        }

        SelectedWaypoint = Waypoints.FirstOrDefault(x => x.PoiId == _selectedPoiId);

        OnPropertyChanged(nameof(SelectedWaypoint));
        RaiseRouteStateChanged();

        if (raiseRouteChanged)
        {
            RouteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void RaiseRouteStateChanged()
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(HasActiveWaypoint));
        OnPropertyChanged(nameof(CurrentWaypointTitle));
        OnPropertyChanged(nameof(CurrentWaypointSubtitle));
        OnPropertyChanged(nameof(CurrentWaypointProgressText));
        OnPropertyChanged(nameof(CurrentWaypointDistanceText));
        OnPropertyChanged(nameof(CurrentWaypointProgressValue));
    }

    public void Dispose()
    {
        _tourRoutePlaybackService.ActiveWaypointChanged -= OnActiveWaypointChanged;
    }
}

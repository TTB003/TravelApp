using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using TravelApp.Models;
using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;
using TravelApp.Services;
using TravelApp.Mobile.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public sealed class MapViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IPoiApiClient _poiApiClient;
    private readonly ILocationProvider _locationProvider;
    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly ILogService _logService;

    private string _statusText = "Đang tải vị trí...";
    private LocationSample? _userLocation;
    private bool _isLoading = true;

    public ObservableCollection<MapPinItem> PoiPins { get; } = [];
    public ObservableCollection<PoiModel> PoisData { get; } = [];

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value) return;
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public LocationSample? UserLocation
    {
        get => _userLocation;
        private set
        {
            if (_userLocation == value) return;
            _userLocation = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public ICommand BackCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand OpenPoiDetailCommand { get; }
    public ICommand OpenHeatmapCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MapViewModel(
        IPoiApiClient poiApiClient,
        ILocationProvider locationProvider,
        ILocalDatabaseService localDatabaseService,
        ILogService logService)
    {
        _poiApiClient = poiApiClient;
        _locationProvider = locationProvider;
        _localDatabaseService = localDatabaseService;
        _logService = logService;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        RefreshCommand = new Command(async () => await LoadDataAsync());
        OpenHeatmapCommand = new Command(async () => await Shell.Current.GoToAsync("PopularPlacesPage"));
        OpenPoiDetailCommand = new Command<MapPinItem>(async pin =>
        {
            if (pin is null) return;

            StatusText = $"Mở chi tiết cho: {pin.Title}";
            await Shell.Current.GoToAsync($"TourDetailPage?tourId={pin.PoiId}");
        });
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsLoading = true;
            StatusText = "Đang lấy vị trí hiện tại...";

            UserLocation = await _locationProvider.GetCurrentLocationAsync(cancellationToken);
            StatusText = UserLocation is null
                ? "Không thể lấy vị trí GPS. Hiển thị dữ liệu gần nhất." 
                : $"Vị trí: {UserLocation.Latitude:F4}, {UserLocation.Longitude:F4}";

            await LoadDataAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Log(nameof(MapViewModel), $"InitializeAsync error: {ex.Message}");
            if (PoisData.Count == 0)
            {
                StatusText = "Lỗi tải dữ liệu. Vui lòng thử lại.";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var language = LocalizationManager.Instance.CurrentLanguage;
            var cachedPois = _userLocation is null
                ? await _localDatabaseService.GetPoisAsync(language, cancellationToken: cancellationToken)
                : await _localDatabaseService.GetPoisAsync(language, _userLocation.Latitude, _userLocation.Longitude, 1500, cancellationToken);

            if (cachedPois.Count > 0)
            {
                ApplyPois(cachedPois.Select(MapCachedPoi).ToList(), "Đang hiển thị dữ liệu từ cache cục bộ.");
            }

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                if (cachedPois.Count == 0)
                {
                    StatusText = "Không có POI nào để hiển thị.";
                }

                return;
            }

            var pois = await _poiApiClient.GetAllAsync(languageCode: language, cancellationToken: cancellationToken);
            if (pois.Count == 0)
            {
                if (cachedPois.Count == 0)
                {
                    StatusText = "Không có POI nào để hiển thị.";
                }

                return;
            }

            await _localDatabaseService.SavePoisAsync(pois.Select(MapPoiToMobileDto), cancellationToken);
            ApplyPois(pois.Select(MapPoi).ToList(), $"Đã tải {pois.Count} POI");
        }
        catch (Exception ex)
        {
            _logService.Log(nameof(MapViewModel), $"LoadDataAsync error: {ex.Message}");
            if (PoisData.Count == 0)
            {
                StatusText = "Lỗi khi tải POI.";
            }
        }
    }

    private void ApplyPois(IReadOnlyList<PoiModel> pois, string statusText)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PoisData.Clear();
            PoiPins.Clear();

            foreach (var poi in pois)
            {
                PoisData.Add(poi);
                PoiPins.Add(new MapPinItem
                {
                    PoiId = poi.Id,
                    Title = poi.Title,
                    Address = poi.Location,
                    Latitude = poi.Latitude,
                    Longitude = poi.Longitude
                });
            }

            StatusText = statusText;
        });
    }

    private PoiModel MapPoi(PoiDto poi)
    {
        var (lat, lng) = ParseLocationCoordinates(poi);

        return new PoiModel
        {
            Id = poi.Id,
            Title = LocalizationManager.GetLocalizedValue(poi.Localizations, nameof(poi.Title)) ?? poi.Title,
            Subtitle = LocalizationManager.GetLocalizedValue(poi.Localizations, nameof(poi.Subtitle)) ?? poi.Subtitle,
            ImageUrl = poi.ImageUrl,
            Location = poi.Location,
            Latitude = lat,
            Longitude = lng,
            Distance = CalculateDistance(poi),
            Duration = poi.Duration ?? "30 min",
            Description = LocalizationManager.GetLocalizedValue(poi.Localizations, nameof(poi.Description)) ?? poi.Description,
            Provider = poi.Provider,
            Credit = poi.Credit
        };
    }

    private static PoiModel MapCachedPoi(PoiMobileDto poi)
    {
        return new PoiModel
        {
            Id = poi.Id,
            Title = poi.Title,
            Subtitle = poi.Subtitle,
            ImageUrl = poi.ImageUrl,
            Location = poi.Location,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            Distance = poi.DistanceMeters.HasValue ? $"{poi.DistanceMeters.Value:F0} m" : string.Empty,
            Duration = string.Empty,
            Description = poi.Description,
            Provider = null,
            Credit = null
        };
    }

    private static PoiMobileDto MapPoiToMobileDto(PoiDto poi)
    {
        return new PoiMobileDto
        {
            Id = poi.Id,
            Title = poi.Title,
            Subtitle = poi.Subtitle ?? string.Empty,
            Description = poi.Description ?? string.Empty,
            LanguageCode = poi.PrimaryLanguage ?? string.Empty,
            PrimaryLanguage = poi.PrimaryLanguage ?? string.Empty,
            ImageUrl = poi.ImageUrl,
            Location = poi.Location,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            GeofenceRadiusMeters = poi.GeofenceRadiusMeters ?? 100,
            Category = poi.Category ?? string.Empty,
            SpeechText = poi.SpeechText,
            SpeechTextLanguageCode = poi.SpeechTextLanguageCode,
            UpdatedAtUtc = poi.UpdatedAtUtc,
            AudioAssets = poi.AudioAssets.Select(x => new PoiAudioMobileDto
            {
                LanguageCode = x.LanguageCode,
                AudioUrl = x.AudioUrl,
                Transcript = x.Transcript,
                IsGenerated = x.IsGenerated
            }).ToList(),
            SpeechTexts = poi.SpeechTexts.Select(x => new PoiSpeechTextMobileDto
            {
                LanguageCode = x.LanguageCode,
                Text = x.Text
            }).ToList()
        };
    }

    private string CalculateDistance(PoiDto poi)
    {
        return "< 5 km";
    }

    private (double lat, double lng) ParseLocationCoordinates(PoiDto poi)
    {
        return poi.Latitude != 0 || poi.Longitude != 0 ? (poi.Latitude, poi.Longitude) : (0, 0);
    }

    public void Dispose()
    {
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

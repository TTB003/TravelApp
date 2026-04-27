using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Models;
using TravelApp.Models.Contracts;
using TravelApp.Services;
using TravelApp.Services.Abstractions;
using TravelApp.Mobile.Services;

namespace TravelApp.ViewModels;

public class ExploreViewModel : INotifyPropertyChanged, IDisposable
{
    private bool _isMenuOpen;
    private bool _isLoggedIn;
    private readonly ITourRouteCatalogService _tourRouteCatalogService;
    private readonly IAudioLibraryService _audioLibraryService;
    private readonly IBookmarkHistoryService _bookmarkHistoryService;
    private int _offlineDownloadsCount;
    private string _selectedBottomTab = "Explore";
    private int? _lastAutoNavigatedPoiId;
    private CancellationTokenSource? _locationMonitoringCts;

    public ObservableCollection<PoiModel> ForYouItems { get; }
    public ObservableCollection<PoiModel> EditorsChoiceItems { get; }
    public ObservableCollection<TourGroupModel> TourGroups { get; } = [];

    public bool IsMenuOpen
    {
        get => _isMenuOpen;
        set
        {
            if (_isMenuOpen == value) return;
            _isMenuOpen = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set
        {
            if (_isLoggedIn == value) return;
            _isLoggedIn = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AuthMenuText));
        }
    }

    public string AuthMenuText => IsLoggedIn ? "Sign Out" : "Sign In";
    public string PurchasesMenuText => _offlineDownloadsCount > 0 ? $"◍  Purchases ({_offlineDownloadsCount})" : "◍  Purchases";
    public bool IsExploreTabActive => string.Equals(_selectedBottomTab, "Explore", StringComparison.Ordinal);
    public bool IsDiscoverTabActive => string.Equals(_selectedBottomTab, "Discover", StringComparison.Ordinal);
    public bool IsMyToursTabActive => string.Equals(_selectedBottomTab, "MyTours", StringComparison.Ordinal);
    public bool IsSavedTabActive => string.Equals(_selectedBottomTab, "Saved", StringComparison.Ordinal);
    public bool IsMenuTabActive => string.Equals(_selectedBottomTab, "Menu", StringComparison.Ordinal);

    public ICommand ToggleMenuCommand { get; }
    public ICommand CloseMenuCommand { get; }
    public ICommand SignInOutCommand { get; }
    public ICommand OpenSearchCommand { get; }
    public ICommand OpenTourDetailCommand { get; }
    public ICommand OpenProfileCommand { get; }
    public ICommand OpenDebugConsoleCommand { get; }
    public ICommand OpenNowPlayingCommand { get; }
    public ICommand OpenMyAudioLibraryCommand { get; }
    public ICommand OpenHistoryCommand { get; }
    public ICommand OpenBookmarksCommand { get; }
    public ICommand OpenTourMapRouteCommand { get; }
    public ICommand OpenMapCommand { get; }
    public ICommand OpenQrScannerCommand { get; }
    public ICommand SelectBottomTabCommand { get; }

    public void ResetBottomTabToExplore()
    {
        if (string.Equals(_selectedBottomTab, "Explore", StringComparison.Ordinal))
        {
            return;
        }

        _selectedBottomTab = "Explore";
        RaiseBottomTabChanged();
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await LoadPoisAsync(cancellationToken);
        await RefreshOfflineDownloadsCountAsync();
    }

    public ExploreViewModel(
        ITourRouteCatalogService tourRouteCatalogService,
        IAudioLibraryService audioLibraryService,
        IBookmarkHistoryService bookmarkHistoryService)
    {
        _tourRouteCatalogService = tourRouteCatalogService;
        _audioLibraryService = audioLibraryService;
        _bookmarkHistoryService = bookmarkHistoryService;
        IsLoggedIn = AuthStateService.IsLoggedIn;
        ForYouItems = [];
        EditorsChoiceItems = [];

        _audioLibraryService.LibraryChanged += async (_, _) => await RefreshOfflineDownloadsCountAsync();

        AuthStateService.AuthStateChanged += (_, _) =>
        {
            IsLoggedIn = AuthStateService.IsLoggedIn;
        };

        // Lắng nghe sự kiện thay đổi ngôn ngữ để cập nhật danh sách POI/Tour ngay lập tức
        LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;

        ToggleMenuCommand = new Command(() => IsMenuOpen = !IsMenuOpen);
        CloseMenuCommand = new Command(() => IsMenuOpen = false);
        SignInOutCommand = new Command(async () =>
        {
            if (IsLoggedIn)
            {
                AuthStateService.IsLoggedIn = false;
            }
            else
            {
                await Shell.Current.GoToAsync("LoginPage");
            }

            IsMenuOpen = false;
        });

        OpenSearchCommand = new Command(async () => await Shell.Current.GoToAsync("SearchPage"));
        OpenProfileCommand = new Command(async () =>
        {
            IsMenuOpen = false;
            await Shell.Current.GoToAsync("ProfilePage");
        });
        OpenDebugConsoleCommand = new Command(async () =>
        {
            IsMenuOpen = false;
            await Shell.Current.GoToAsync("DebugRuntimeConsolePage");
        });
        OpenNowPlayingCommand = new Command(async () => await Shell.Current.GoToAsync("NowPlayingPage"));
        OpenMyAudioLibraryCommand = new Command(async () =>
        {
            IsMenuOpen = false;
            await Shell.Current.GoToAsync("MyAudioLibraryPage");
        });
        OpenHistoryCommand = new Command(async () =>
        {
            IsMenuOpen = false;
            await Shell.Current.GoToAsync("BookmarksHistoryPage?tab=history");
        });
        OpenBookmarksCommand = new Command(async () =>
        {
            IsMenuOpen = false;
            await Shell.Current.GoToAsync("BookmarksHistoryPage?tab=bookmarks");
        });
        OpenTourMapRouteCommand = new Command<int>(async (id) => await Shell.Current.GoToAsync($"TourMapRoutePage?tourId={id}"));
        OpenMapCommand = new Command(async () => await Shell.Current.GoToAsync("MapPage"));
        OpenQrScannerCommand = new Command(async () => await Shell.Current.GoToAsync("QrScannerPage"));
        SelectBottomTabCommand = new Command<string>(async tab => await SelectBottomTabAsync(tab));
        OpenTourDetailCommand = new Command<PoiModel>(async item =>
        {
            if (item is null) return;
            // Cập nhật ID cuối cùng ngay cả khi người dùng nhấn thủ công để tránh việc tự động nhảy trang
            _lastAutoNavigatedPoiId = item.Id;

            // Allow anonymous users to view tour details. Only require authentication for owner-specific actions.
            await _bookmarkHistoryService.AddHistoryAsync(item);
            await Shell.Current.GoToAsync($"TourDetailPage?tourId={item.Id}");
        });

        _ = RefreshAsync();
        _ = StartProximityMonitoring();
    }

    private async Task StartProximityMonitoring()
    {
        _locationMonitoringCts?.Cancel();
        _locationMonitoringCts = new CancellationTokenSource();
        var token = _locationMonitoringCts.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                // Chỉ thực hiện theo dõi khi đang ở tab Explore và không mở Menu
                if (IsExploreTabActive && !IsMenuOpen)
                {
                    var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)), token);
                    if (location != null)
                    {
                        var allItems = ForYouItems.Concat(EditorsChoiceItems).ToList();

                        // 1. Tìm POI gần nhất tuyệt đối so với vị trí hiện tại của người dùng
                        var closestPoiInfo = allItems
                            .Select(poi => new { Poi = poi, Distance = location.CalculateDistance(poi.Latitude, poi.Longitude, DistanceUnits.Kilometers) * 1000 })
                            .OrderBy(x => x.Distance)
                            .FirstOrDefault();

                        // 2. Nếu tìm thấy POI và nó nằm trong bán kính kích hoạt (100m)
                        if (closestPoiInfo != null && closestPoiInfo.Distance <= 100)
                        {
                            var targetPoi = closestPoiInfo.Poi;

                            // 3. Điều kiện để tự động điều hướng:
                            // - POI gần nhất này phải khác với POI hệ thống vừa mới mở (tránh việc tự nhảy lại trang khi đang ở POI hiện tại)
                            // - VÀ POI này chưa được phát trong vòng 20 phút qua (kiểm tra Cooldown)
                            if (targetPoi.Id != _lastAutoNavigatedPoiId && !TourDetailViewModel.IsInCooldown(targetPoi.Id))
                            {
                                _lastAutoNavigatedPoiId = targetPoi.Id;

                                await MainThread.InvokeOnMainThreadAsync(async () => 
                                {
                                    // Tự động mở trang và phát Audio cho POI mới chiếm ưu thế về khoảng cách
                                    await Shell.Current.GoToAsync($"TourDetailPage?tourId={targetPoi.Id}&autoplay=true");
                                });
                            }
                        }
                    }
                }
                await Task.Delay(5000, token); // Kiểm tra mỗi 5 giây
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Proximity monitoring error: {ex.Message}");
        }
    }

    private async Task SelectBottomTabAsync(string? tab)
    {
        if (string.IsNullOrWhiteSpace(tab))
        {
            return;
        }

        _selectedBottomTab = tab;
        RaiseBottomTabChanged();

        switch (tab)
        {
            case "Discover":
                break;
            case "MyTours":
                await Shell.Current.GoToAsync("MapPage");
                break;
            case "Saved":
                await Shell.Current.GoToAsync("BookmarksHistoryPage?tab=bookmarks");
                break;
            case "Menu":
                IsMenuOpen = true;
                break;
        }
    }

    private void RaiseBottomTabChanged()
    {
        OnPropertyChanged(nameof(IsExploreTabActive));
        OnPropertyChanged(nameof(IsDiscoverTabActive));
        OnPropertyChanged(nameof(IsMyToursTabActive));
        OnPropertyChanged(nameof(IsSavedTabActive));
        OnPropertyChanged(nameof(IsMenuTabActive));
    }

    private async Task RefreshOfflineDownloadsCountAsync()
    {
        var count = await _audioLibraryService.GetDownloadedCountAsync(UserProfileService.PreferredLanguage);
        if (_offlineDownloadsCount == count)
        {
            return;
        }

        _offlineDownloadsCount = count;
        MainThread.BeginInvokeOnMainThread(() => OnPropertyChanged(nameof(PurchasesMenuText)));
    }

    private async Task LoadPoisAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var language = UserProfileService.PreferredLanguage;
            var routes = await _tourRouteCatalogService.GetAllRoutesAsync(language, cancellationToken);

            // Chuẩn bị dữ liệu ở Background Thread trước
            var orderedRoutes = routes.OrderBy(x => x.Id).ToList();
            var forYouRoute = orderedRoutes.FirstOrDefault();
            var editorsChoiceRoute = orderedRoutes.Skip(1).FirstOrDefault();

            var forYou = BuildRouteItems(forYouRoute);
            var editors = BuildRouteItems(editorsChoiceRoute);
            
            var tourGroupsData = routes.Where(r => r.Waypoints.Any()).Select(route => new TourGroupModel
            {
                Id = route.Id,
                Name = route.Name,
                Items = new ObservableCollection<PoiModel>(route.Waypoints.Select((w, i) => MapPoi(w.Poi, route.Name, i + 1)))
            }).ToList();

            // Chỉ nhảy về MainThread để gán dữ liệu cuối cùng
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Dùng cơ chế Clear/Add nhưng gộp lại để giảm số lần Notify
                ForYouItems.Clear();
                foreach (var item in forYou) ForYouItems.Add(item);

                EditorsChoiceItems.Clear();
                foreach (var item in editors) EditorsChoiceItems.Add(item);

                TourGroups.Clear();
                foreach (var group in tourGroupsData) TourGroups.Add(group);
            });
        }
        catch
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ForYouItems.Clear();
                EditorsChoiceItems.Clear();

            });
        }
    }

    private static List<PoiModel> BuildRouteItems(TourRouteDto? route)
    {
        if (route is null)
        {
            return [];
        }

        return route.Waypoints
            .Select((waypoint, index) => MapPoi(waypoint.Poi, route.Name, index + 1))
            .GroupBy(x => x.Id)
            .Select(g => g.First())
            .ToList();
    }

    private static PoiModel MapPoi(PoiMobileDto dto, string? tourName, int? step = null)
    {
        return new PoiModel
        {
            Id = dto.Id,
            Title = dto.Title,
            Subtitle = dto.Subtitle,
            ImageUrl = dto.ImageUrl,
            Location = dto.Location,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Distance = dto.DistanceMeters.HasValue ? $"{dto.DistanceMeters.Value:F0} m" : string.Empty,
            Description = dto.Description,
            Provider = string.Empty,
            Credit = string.IsNullOrWhiteSpace(tourName) ? string.Empty : tourName,
            SpeechText = dto.SpeechText,
            Duration = step.HasValue ? $"STEP {step}" : string.Empty
        };
    }

    public class TourGroupModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ObservableCollection<PoiModel> Items { get; set; }
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        // LocalizationManager sử dụng string.Empty để báo hiệu toàn bộ thuộc tính (bao gồm ngôn ngữ) thay đổi
        if (string.IsNullOrEmpty(e.PropertyName))
        {
            // Gọi Refresh để nạp lại dữ liệu từ API/Cache theo ngôn ngữ mới
            _ = RefreshAsync();
        }
    }

    public void Dispose()
    {
        _locationMonitoringCts?.Cancel();
        _locationMonitoringCts?.Dispose();
        LocalizationManager.Instance.PropertyChanged -= OnLocalizationChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

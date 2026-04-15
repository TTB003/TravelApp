using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Models;
using TravelApp.Models.Contracts;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public class ExploreViewModel : INotifyPropertyChanged
{
    private bool _isMenuOpen;
    private bool _isLoggedIn;
    private readonly ITourRouteCatalogService _tourRouteCatalogService;
    private readonly IAudioLibraryService _audioLibraryService;
    private readonly IBookmarkHistoryService _bookmarkHistoryService;
    private int _offlineDownloadsCount;
    private string _selectedBottomTab = "Explore";

    public ObservableCollection<PoiModel> ForYouItems { get; }
    public ObservableCollection<PoiModel> EditorsChoiceItems { get; }

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
        OpenTourMapRouteCommand = new Command(async () => await Shell.Current.GoToAsync("MapPage"));
        OpenMapCommand = new Command(async () => await Shell.Current.GoToAsync("MapPage"));
        OpenQrScannerCommand = new Command(async () => await Shell.Current.GoToAsync("QrScannerPage"));
        SelectBottomTabCommand = new Command<string>(async tab => await SelectBottomTabAsync(tab));
        OpenTourDetailCommand = new Command<PoiModel>(async item =>
        {
            if (item is null) return;

            // Allow anonymous users to view tour details. Only require authentication for owner-specific actions.
            await _bookmarkHistoryService.AddHistoryAsync(item);
            await Shell.Current.GoToAsync($"TourDetailPage?tourId={item.Id}");
        });

        _ = RefreshAsync();
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

            var orderedRoutes = routes.OrderBy(x => x.Id).ToList();
            var forYouRoute = orderedRoutes.FirstOrDefault();
            var editorsChoiceRoute = orderedRoutes.Skip(1).FirstOrDefault();

            var forYou = BuildRouteItems(forYouRoute);
            var editors = BuildRouteItems(editorsChoiceRoute);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ForYouItems.Clear();
                foreach (var item in forYou)
                {
                    ForYouItems.Add(item);
                }

                EditorsChoiceItems.Clear();
                foreach (var item in editors)
                {
                    EditorsChoiceItems.Add(item);
                }
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
            .Select(waypoint => MapPoi(waypoint.Poi, route.Name))
            .GroupBy(x => x.Id)
            .Select(g => g.First())
            .ToList();
    }

    private static PoiModel MapPoi(PoiMobileDto dto, string? tourName)
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
            Duration = string.Empty,
            Description = dto.Description,
            Provider = string.Empty,
            Credit = string.IsNullOrWhiteSpace(tourName) ? string.Empty : tourName,
            SpeechText = dto.SpeechText
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

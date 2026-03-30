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
    private readonly IPoiApiClient _poiApiClient;

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

    public ICommand ToggleMenuCommand { get; }
    public ICommand CloseMenuCommand { get; }
    public ICommand SignInOutCommand { get; }
    public ICommand OpenSearchCommand { get; }
    public ICommand OpenTourDetailCommand { get; }
    public ICommand OpenProfileCommand { get; }
    public ICommand OpenDebugConsoleCommand { get; }

    public ExploreViewModel(IPoiApiClient poiApiClient)
    {
        _poiApiClient = poiApiClient;
        IsLoggedIn = AuthStateService.IsLoggedIn;
        ForYouItems = [];
        EditorsChoiceItems = [];

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
        OpenTourDetailCommand = new Command<PoiModel>(async item =>
        {
            if (item is null) return;
            await Shell.Current.GoToAsync($"TourDetailPage?tourId={item.Id}");
        });

        _ = LoadPoisAsync();
    }

    private async Task LoadPoisAsync()
    {
        try
        {
            var language = UserProfileService.PreferredLanguage;
            var pois = await _poiApiClient.GetAllAsync(language);

            var mapped = pois.Select(MapPoi).ToList();
            var forYou = mapped.Take(3).ToList();
            var editors = mapped.Skip(3).Take(3).ToList();

            if (forYou.Count == 0 && editors.Count == 0)
            {
                forYou = MockDataService.GetForYouData();
                editors = MockDataService.GetEditorsChoiceData();
            }

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
                foreach (var item in MockDataService.GetForYouData())
                {
                    ForYouItems.Add(item);
                }

                EditorsChoiceItems.Clear();
                foreach (var item in MockDataService.GetEditorsChoiceData())
                {
                    EditorsChoiceItems.Add(item);
                }
            });
        }
    }

    private static PoiModel MapPoi(PoiDto dto)
    {
        return new PoiModel
        {
            Id = dto.Id,
            Title = dto.Title,
            Subtitle = dto.Subtitle,
            ImageUrl = dto.ImageUrl,
            Location = dto.Location,
            Rating = dto.Rating,
            ReviewCount = dto.ReviewCount,
            Price = dto.Price,
            Distance = dto.Distance,
            Duration = dto.Duration,
            Description = dto.Description,
            Provider = dto.Provider,
            Credit = dto.Credit
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

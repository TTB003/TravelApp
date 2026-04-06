using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Models;
using TravelApp.Models.Contracts;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public class TourDetailViewModel : INotifyPropertyChanged
{
    private PoiModel? _tour;
    private readonly IPoiApiClient _poiApiClient;
    private readonly ITextToSpeechService _ttsService;
    private readonly IList<string> _languages = new List<string> { "en", "vi", "fr", "ja", "es" };
    private string _selectedLanguage = UserProfileService.PreferredLanguage;
    private TabSelection _selectedTab = TabSelection.Description;
    private readonly string[] _supportedLanguages = new[] { "en", "vi" };
    private int _languageIndex = 0;

    public PoiModel? Tour
    {
        get => _tour;
        private set
        {
            _tour = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProviderName));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(Credit));
        }
    }

    public string ProviderName => Tour?.Provider ?? "TravelApp";
    public string Description => Tour?.Description ?? "This tour is available daily and includes the most iconic landmarks in the area.";
    public string Credit => Tour?.Credit ?? string.Empty;

    public IList<string> Languages => (IList<string>)_languages;

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (_selectedLanguage == value) return;
            _selectedLanguage = value;
            UserProfileService.PreferredLanguage = _selectedLanguage;
            OnPropertyChanged();
            // reload POI in selected language
            if (Tour is not null)
            {
                _ = LoadAsync(Tour.Id);
            }
        }
    }

    public string DescriptionTextColor => _selectedTab == TabSelection.Description ? "#D31963" : "#94959B";
    public string StoriesTextColor => _selectedTab == TabSelection.Stories ? "#D31963" : "#94959B";
    public string ReviewsTextColor => _selectedTab == TabSelection.Reviews ? "#D31963" : "#94959B";

    public bool IsDescriptionVisible => _selectedTab == TabSelection.Description;
    public bool IsStoriesVisible => _selectedTab == TabSelection.Stories;
    public bool IsReviewsVisible => _selectedTab == TabSelection.Reviews;

    public ICommand SelectDescriptionCommand { get; }
    public ICommand SelectStoriesCommand { get; }
    public ICommand SelectReviewsCommand { get; }
    public ICommand ChangeLanguageCommand { get; }
    public ICommand PlayAudioCommand { get; }

    public ICommand BackCommand { get; }

    public TourDetailViewModel(IPoiApiClient poiApiClient, ITextToSpeechService ttsService)
    {
        _poiApiClient = poiApiClient;
        _ttsService = ttsService;
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));

        SelectDescriptionCommand = new Command(() => SelectTab(TabSelection.Description));
        SelectStoriesCommand = new Command(() => SelectTab(TabSelection.Stories));
        SelectReviewsCommand = new Command(() => SelectTab(TabSelection.Reviews));
        ChangeLanguageCommand = new Command(async () => await CycleLanguageAsync());
        PlayAudioCommand = new Command(async () => await PlayPoiAudioAsync());
    }

    public void Load(string? tourId)
    {
        if (!int.TryParse(tourId, out var id))
            return;

        _ = LoadAsync(id);
    }

    private void SelectTab(TabSelection tab)
    {
        if (_selectedTab == tab) return;
        _selectedTab = tab;
        OnPropertyChanged(nameof(DescriptionTextColor));
        OnPropertyChanged(nameof(StoriesTextColor));
        OnPropertyChanged(nameof(ReviewsTextColor));
        OnPropertyChanged(nameof(IsDescriptionVisible));
        OnPropertyChanged(nameof(IsStoriesVisible));
        OnPropertyChanged(nameof(IsReviewsVisible));
    }

    private async Task CycleLanguageAsync()
    {
        // rotate supported languages
        var current = _languages.IndexOf(_selectedLanguage);
        var next = (current + 1) % _languages.Count;
        SelectedLanguage = _languages[next];
    }

    private enum TabSelection
    {
        Description,
        Stories,
        Reviews
    }

    private async Task LoadAsync(int id)
    {
        try
        {
            var dto = await _poiApiClient.GetByIdAsync(id, SelectedLanguage);
            if (dto is not null)
            {
                Tour = MapPoi(dto);
                return;
            }
        }
        catch
        {
            // ignore, leave Tour as null
        }

        // If API failed or returned nothing, leave Tour as null so the UI can show an appropriate empty state
        Tour = null;
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
            ,
            Stories = dto.Stories?.Select(s => new TravelApp.Models.Contracts.Story
            {
                Title = s.Title,
                Content = s is null ? string.Empty : s is { } ? s.Content : string.Empty,
                LanguageCode = s is null ? "en" : s.LanguageCode
            }).ToList() ?? new List<TravelApp.Models.Contracts.Story>()
        };
    }

    private async Task PlayPoiAudioAsync()
    {
        if (Tour is null) return;
        var text = Tour.Description ?? Tour.Title;
        try
        {
            await _ttsService.PlayTextAsync(text, SelectedLanguage);
        }
        catch
        {
            // ignore playback failures here
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

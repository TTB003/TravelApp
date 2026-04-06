using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using TravelApp.Models;
using TravelApp.Models.Contracts;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public class TourDetailViewModel : INotifyPropertyChanged
{
    private PoiModel? _tour;
    private readonly IPoiApiClient _poiService;
    private readonly ITextToSpeechService _ttsService;
    private CancellationTokenSource? _reloadCts;
    private readonly IList<string> _languages = new List<string> { "vi", "en", "fr" };
    private string _selectedLanguage = UserProfileService.PreferredLanguage;
    private TabSelection _selectedTab = TabSelection.Description;
    private int _currentPoiId;

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

            // reload POI content and stories in the new language
            if (_currentPoiId != 0)
            {
                // cancel previous reload if any and start a fresh reload that will also trigger TTS
                _reloadCts?.Cancel();
                _reloadCts = new CancellationTokenSource();
                var ct = _reloadCts.Token;
                _ = ReloadForLanguageAsync(_currentPoiId, ct);
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

    public ObservableCollection<Story> Stories { get; } = new();

    public TourDetailViewModel(IPoiApiClient poiApiClient, ITextToSpeechService ttsService)
    {
        _poiService = poiApiClient;
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
        _currentPoiId = id;
        await ReloadForLanguageAsync(id, CancellationToken.None);
    }

    private async Task ReloadForLanguageAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _poiService.GetByIdAsync(id, SelectedLanguage, cancellationToken);
            if (dto is not null)
            {
                Tour = MapPoi(dto);
                UpdateStories(dto);

                // After data refreshed, play first story description via TTS (if available)
                try
                {
                    var first = Stories.FirstOrDefault();
                    if (first is not null && !string.IsNullOrWhiteSpace(first.Content))
                    {
                        // Do not await to avoid blocking UI; allow cancellation token to stop playback if needed
                        // Prefer AudioUrl if available; otherwise use TTS on Description
                        if (!string.IsNullOrWhiteSpace(first.AudioUrl))
                        {
                            // If audio url existed we would hand over to AudioService; here we fallback to TTS since audio playback service is separate
                            _ = _ttsService.PlayTextAsync(first.Description ?? first.Content, SelectedLanguage, cancellationToken);
                        }
                        else
                        {
                            _ = _ttsService.PlayTextAsync(first.Description ?? first.Content, SelectedLanguage, cancellationToken);
                        }
                    }
                }
                catch
                {
                    // swallow TTS errors to avoid crashing UI
                }

                return;
            }
        }
        catch
        {
            // ignore, leave Tour as null
        }

        // If API failed or returned nothing, leave Tour as null so the UI can show an appropriate empty state
        Tour = null;
        Stories.Clear();
    }

    private void UpdateStories(PoiDto dto)
    {
        Stories.Clear();
        if (dto.Stories is null) return;

        foreach (var s in dto.Stories)
        {
            if (s is null) continue;
            Stories.Add(new Story
            {
                Title = s.Title,
                Content = s.Content,
                LanguageCode = s.LanguageCode,
                AudioUrl = s is { } ? s.AudioUrl : null,
                Description = s is { } ? s.Content : string.Empty
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

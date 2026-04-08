using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Text;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public class SearchViewModel : INotifyPropertyChanged
{
    private readonly List<SearchDestinationItem> _allDestinations = [];
    private string _searchQuery = string.Empty;
    private bool _isFilterOpen;
    private bool _popularMostRated;
    private bool _tourEnabled = true;
    private bool _museumEnabled = true;
    private bool _questEnabled = true;
    private readonly IPoiApiClient _poiApiClient;

    public ObservableCollection<SearchDestinationItem> PopularDestinations { get; }
    public ObservableCollection<SearchDestinationItem> SearchResults { get; }
    public ObservableCollection<TourTypeOption> TourTypes { get; }

    public string SearchHeaderText => string.IsNullOrWhiteSpace(SearchQuery) ? "Popular Destinations" : "Search Results";

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery == value)
            {
                return;
            }

            _searchQuery = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SearchHeaderText));
            RebuildSearchResults();
        }
    }

    public bool IsFilterOpen
    {
        get => _isFilterOpen;
        set
        {
            if (_isFilterOpen == value) return;
            _isFilterOpen = value;
            OnPropertyChanged();
        }
    }

    public bool PopularMostRated
    {
        get => _popularMostRated;
        set
        {
            if (_popularMostRated == value) return;
            _popularMostRated = value;
            OnPropertyChanged();
        }
    }

    public bool TourEnabled
    {
        get => _tourEnabled;
        set
        {
            if (_tourEnabled == value) return;
            _tourEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool MuseumEnabled
    {
        get => _museumEnabled;
        set
        {
            if (_museumEnabled == value) return;
            _museumEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool QuestEnabled
    {
        get => _questEnabled;
        set
        {
            if (_questEnabled == value) return;
            _questEnabled = value;
            OnPropertyChanged();
        }
    }

    public ICommand BackCommand { get; }
    public ICommand OpenFilterCommand { get; }
    public ICommand CloseFilterCommand { get; }
    public ICommand ApplyFilterCommand { get; }
    public ICommand ClearFiltersCommand { get; }
    public ICommand ToggleTourTypeCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand OpenDestinationCommand { get; }

    public SearchViewModel(IPoiApiClient poiApiClient)
    {
        _poiApiClient = poiApiClient;
        PopularDestinations = [];
        SearchResults = [];
        TourTypes = new ObservableCollection<TourTypeOption>
        {
            new() { Name = "Car tour", IsSelected = true },
            new() { Name = "Walking tour", IsSelected = true },
            new() { Name = "Bike tour", IsSelected = true },
            new() { Name = "Bus tour", IsSelected = true },
            new() { Name = "Boat tour", IsSelected = true },
            new() { Name = "Running tour", IsSelected = true },
            new() { Name = "Train tour", IsSelected = true },
            new() { Name = "Horse riding tour", IsSelected = true }
        };

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        OpenFilterCommand = new Command(() => IsFilterOpen = true);
        CloseFilterCommand = new Command(() => IsFilterOpen = false);
        ApplyFilterCommand = new Command(() => IsFilterOpen = false);
        SearchCommand = new Command(() => ApplySearch());
        OpenDestinationCommand = new Command<SearchDestinationItem>(async item => await OpenDestinationAsync(item));
        ToggleTourTypeCommand = new Command<TourTypeOption>(option =>
        {
            if (option is null) return;
            option.IsSelected = !option.IsSelected;
        });
        ClearFiltersCommand = new Command(() =>
        {
            PopularMostRated = false;
            TourEnabled = false;
            MuseumEnabled = false;
            QuestEnabled = false;
            foreach (var type in TourTypes)
            {
                type.IsSelected = false;
            }
        });

        _ = LoadDestinationsAsync();
    }

    private async Task LoadDestinationsAsync()
    {
        try
        {
            var language = UserProfileService.PreferredLanguage;
            var pois = await _poiApiClient.GetAllAsync(language);

            // Group POIs by location and create destination items
            var destinations = pois
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? p.Subtitle : p.Category)
                .Select(g => new SearchDestinationItem
                {
                    Name = g.Key ?? "Unknown",
                    Type = "DESTINATION",
                    Count = g.Count(),
                    ImageUrl = g.FirstOrDefault()?.ImageUrl ?? "https://placehold.co/1200x600/png?text=Travel+App",
                    FirstPoiId = g.MinBy(p => p.Id)?.Id ?? 0,
                    SearchText = string.Join(" ", g.SelectMany(p => new[] { p.Title, p.Subtitle, p.Description, p.Location, p.Category }).Where(x => !string.IsNullOrWhiteSpace(x)))
                })
                .ToList();

            if (destinations.Count == 0)
            {
                LoadMockDestinations();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _allDestinations.Clear();
                    _allDestinations.AddRange(destinations);

                    PopularDestinations.Clear();
                    foreach (var destination in destinations.Take(2))
                    {
                        PopularDestinations.Add(destination);
                    }

                    RebuildSearchResults();
                });
            }
        }
        catch
        {
            LoadMockDestinations();
        }
    }

    private void LoadMockDestinations()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _allDestinations.Clear();
            PopularDestinations.Clear();
            var hcm = new SearchDestinationItem { Name = "🍲 Ho Chi Minh Food Tour", Type = "DESTINATION", Count = 3, ImageUrl = "https://placehold.co/1200x600/png?text=Ho+Chi+Minh+Food+Tour", FirstPoiId = 1, SearchText = "Chợ Bến Thành Phở Vĩnh Khánh Bến Bạch Đằng Ho Chi Minh Food Tour" };
            var hanoi = new SearchDestinationItem { Name = "🍜 Hanoi Food Tour", Type = "DESTINATION", Count = 3, ImageUrl = "https://placehold.co/1200x600/png?text=Hanoi+Food+Tour", FirstPoiId = 4, SearchText = "Chùa Một Cột Phố Hàng Xanh Phố Hàng Dâu Hanoi Food Tour" };
            _allDestinations.Add(hcm);
            _allDestinations.Add(hanoi);
            PopularDestinations.Add(hcm);
            PopularDestinations.Add(hanoi);

            RebuildSearchResults();
        });
    }

    private void ApplySearch()
    {
        OnPropertyChanged(nameof(SearchHeaderText));
        RebuildSearchResults();
        IsFilterOpen = false;
    }

    private void RebuildSearchResults()
    {
        var query = NormalizeText(SearchQuery);
        IReadOnlyList<SearchDestinationItem> source = string.IsNullOrWhiteSpace(query)
            ? PopularDestinations.ToList()
            : _allDestinations.Where(item => NormalizeText(item.SearchText).Contains(query, StringComparison.OrdinalIgnoreCase) || NormalizeText(item.Name).Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            SearchResults.Clear();
            foreach (var item in source)
            {
                SearchResults.Add(item);
            }
        });
    }

    private async Task OpenDestinationAsync(SearchDestinationItem? item)
    {
        if (item is null || item.FirstPoiId <= 0)
        {
            return;
        }

        await Shell.Current.GoToAsync($"TourDetailPage?tourId={item.FirstPoiId}");
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new System.Text.StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class SearchDestinationItem
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public int Count { get; set; }
    public required string ImageUrl { get; set; }
    public int FirstPoiId { get; set; }
    public string SearchText { get; set; } = string.Empty;
}

public class TourTypeOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public required string Name { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

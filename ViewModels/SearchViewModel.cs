using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TravelApp.ViewModels;

public class SearchViewModel : INotifyPropertyChanged
{
    private bool _isFilterOpen;
    private bool _popularMostRated;
    private bool _tourEnabled = true;
    private bool _museumEnabled = true;
    private bool _questEnabled = true;

    public ObservableCollection<SearchDestinationItem> PopularDestinations { get; }
    public ObservableCollection<TourTypeOption> TourTypes { get; }

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

    public SearchViewModel()
    {
        PopularDestinations = new ObservableCollection<SearchDestinationItem>
        {
            new() { Name = "London", Type = "CITY", Count = 3, ImageUrl = "https://images.unsplash.com/photo-1533929736458-ca588d08c8be?w=1200&h=600&fit=crop" },
            new() { Name = "Paris", Type = "CITY", Count = 1, ImageUrl = "https://images.unsplash.com/photo-1431274172761-fca41d930114?w=1200&h=600&fit=crop" },
            new() { Name = "Seoul", Type = "CITY", Count = 2, ImageUrl = "https://images.unsplash.com/photo-1538669715315-155098f0fb1d?w=1200&h=600&fit=crop" },
            new() { Name = "Rome", Type = "CITY", Count = 4, ImageUrl = "https://images.unsplash.com/photo-1525874684015-58379d421a52?w=1200&h=600&fit=crop" }
        };

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

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

    public ICommand BackCommand { get; }

    public TourDetailViewModel(IPoiApiClient poiApiClient)
    {
        _poiApiClient = poiApiClient;
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public void Load(string? tourId)
    {
        if (!int.TryParse(tourId, out var id))
            return;

        _ = LoadAsync(id);
    }

    private async Task LoadAsync(int id)
    {
        try
        {
            var dto = await _poiApiClient.GetByIdAsync(id);
            if (dto is not null)
            {
                Tour = MapPoi(dto);
                return;
            }
        }
        catch
        {
        }

        Tour = MockDataService.GetById(id);
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

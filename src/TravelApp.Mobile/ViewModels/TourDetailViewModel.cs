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
    private PoiDto? _currentPoiDto;
    private string _speechTextInput = string.Empty;
    private bool _isSavingSpeechText;
    private readonly IPoiApiClient _poiApiClient;
    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly TravelApp.Services.Runtime.TourRouteCacheService _tourRouteCacheService;

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
            OnPropertyChanged(nameof(SpeechTextInput));
        }
    }

    public string SpeechTextInput
    {
        get => _speechTextInput;
        set
        {
            if (_speechTextInput == value)
            {
                return;
            }

            _speechTextInput = value;
            OnPropertyChanged();
        }
    }

    public bool IsSavingSpeechText
    {
        get => _isSavingSpeechText;
        private set
        {
            if (_isSavingSpeechText == value)
            {
                return;
            }

            _isSavingSpeechText = value;
            OnPropertyChanged();
        }
    }

    public string ProviderName => Tour?.Provider ?? "TravelApp";
    public string Description => Tour?.SpeechText ?? Tour?.Description ?? "This tour is available daily and includes the most iconic landmarks in the area.";
    public string Credit => Tour?.Credit ?? string.Empty;

    public ICommand BackCommand { get; }
    public ICommand ViewTourCommand { get; }
    public ICommand SaveSpeechTextCommand { get; }

    public TourDetailViewModel(IPoiApiClient poiApiClient, ILocalDatabaseService localDatabaseService, TravelApp.Services.Runtime.TourRouteCacheService tourRouteCacheService)
    {
        _poiApiClient = poiApiClient;
        _localDatabaseService = localDatabaseService;
        _tourRouteCacheService = tourRouteCacheService;
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        ViewTourCommand = new Command(async () =>
        {
            if (Tour is null)
            {
                return;
            }

            await SaveSpeechTextAsync(showConfirmation: false);
            await Shell.Current.GoToAsync($"TourMapRoutePage?tourId={Tour.Id}&poiId={Tour.Id}");
        });
        SaveSpeechTextCommand = new Command(async () => await SaveSpeechTextAsync());
    }

    public void Load(string? tourId)
    {
        if (!int.TryParse(tourId, out var id))
            return;

        _ = LoadAsync(id);
    }

    private async Task LoadAsync(int id)
    {
        var localPoi = MockDataService.GetById(id);

        try
        {
            var dto = await _poiApiClient.GetByIdAsync(id, UserProfileService.PreferredLanguage);
            if (dto is not null)
            {
                if (IsStaleCentralParkPoi(dto) && localPoi is not null)
                {
                    _currentPoiDto = MergePoiDto(dto, localPoi);
                    Tour = localPoi;
                    SpeechTextInput = localPoi.SpeechText ?? localPoi.Description ?? string.Empty;
                    return;
                }

                _currentPoiDto = dto;
                Tour = MapPoi(dto);
                SpeechTextInput = dto.SpeechText ?? dto.Description ?? string.Empty;
                return;
            }
        }
        catch
        {
        }

        _currentPoiDto = null;
        if (localPoi is not null)
        {
            Tour = localPoi;
            _currentPoiDto = BuildPoiDtoFromLocalPoi(localPoi);
            SpeechTextInput = localPoi.SpeechText ?? localPoi.Description ?? string.Empty;
            return;
        }

        Tour = null;
        SpeechTextInput = string.Empty;
    }

    private async Task SaveSpeechTextAsync(bool showConfirmation = true)
    {
        if (Tour is null || _currentPoiDto is null || IsSavingSpeechText)
        {
            return;
        }

        IsSavingSpeechText = true;
        try
        {
            var speechText = SpeechTextInput?.Trim();
            var request = new UpsertPoiRequestDto(
                _currentPoiDto.Title,
                _currentPoiDto.Subtitle,
                _currentPoiDto.ImageUrl,
                _currentPoiDto.Location,
                _currentPoiDto.Latitude,
                _currentPoiDto.Longitude,
                _currentPoiDto.GeofenceRadiusMeters,
                _currentPoiDto.Description,
                _currentPoiDto.Category,
                _currentPoiDto.PrimaryLanguage,
                _currentPoiDto.Duration,
                _currentPoiDto.Provider,
                _currentPoiDto.Credit,
                speechText,
                _currentPoiDto.Localizations,
                _currentPoiDto.AudioAssets);

            await _localDatabaseService.SavePoisAsync([
                new PoiMobileDto
                {
                    Id = _currentPoiDto.Id,
                    Title = _currentPoiDto.Title,
                    Subtitle = _currentPoiDto.Subtitle,
                    Description = _currentPoiDto.Description,
                    LanguageCode = _currentPoiDto.PrimaryLanguage,
                    PrimaryLanguage = _currentPoiDto.PrimaryLanguage,
                    ImageUrl = _currentPoiDto.ImageUrl,
                    Location = _currentPoiDto.Location,
                    Latitude = _currentPoiDto.Latitude,
                    Longitude = _currentPoiDto.Longitude,
                    GeofenceRadiusMeters = _currentPoiDto.GeofenceRadiusMeters ?? 100,
                    Category = _currentPoiDto.Category ?? string.Empty,
                    SpeechText = speechText,
                    AudioAssets = _currentPoiDto.AudioAssets.Select(audio => new PoiAudioMobileDto
                    {
                        LanguageCode = audio.LanguageCode,
                        AudioUrl = audio.AudioUrl,
                        Transcript = audio.Transcript,
                        IsGenerated = audio.IsGenerated
                    }).ToList()
                }
            ], CancellationToken.None);

            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                await _poiApiClient.UpdateAsync(_currentPoiDto.Id, request);
            }

            await _tourRouteCacheService.InvalidateAsync(_currentPoiDto.Id, null, CancellationToken.None);

            _currentPoiDto.SpeechText = speechText;
            Tour.SpeechText = speechText;
            OnPropertyChanged(nameof(Tour));
            OnPropertyChanged(nameof(Description));
            SpeechTextInput = speechText ?? string.Empty;
            if (showConfirmation)
            {
                await Shell.Current.DisplayAlert("Saved", "Text to speech đã được lưu.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Không lưu được text TTS: {ex.Message}", "OK");
        }
        finally
        {
            IsSavingSpeechText = false;
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
            Distance = dto.Distance,
            Duration = dto.Duration,
            Description = dto.Description,
            Provider = dto.Provider,
            Credit = dto.Credit,
            SpeechText = dto.SpeechText
        };
    }

    private static bool IsStaleCentralParkPoi(PoiDto dto)
    {
        return ContainsCentralParkText(dto.Title)
               || ContainsCentralParkText(dto.Description)
               || ContainsCentralParkText(dto.Location);
    }

    private static bool ContainsCentralParkText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("Central Park", StringComparison.OrdinalIgnoreCase)
               || value.Contains("New York", StringComparison.OrdinalIgnoreCase)
               || value.Contains("USA", StringComparison.OrdinalIgnoreCase);
    }

    private static PoiDto MergePoiDto(PoiDto source, PoiModel localPoi)
    {
        return new PoiDto
        {
            Id = source.Id,
            Title = localPoi.Title,
            Subtitle = localPoi.Subtitle,
            ImageUrl = localPoi.ImageUrl,
            Location = localPoi.Location,
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            GeofenceRadiusMeters = source.GeofenceRadiusMeters,
            Distance = source.Distance,
            Duration = localPoi.Duration,
            Description = localPoi.Description,
            Provider = localPoi.Provider,
            Credit = localPoi.Credit,
            Category = source.Category,
            PrimaryLanguage = source.PrimaryLanguage,
            SpeechText = localPoi.SpeechText ?? source.SpeechText ?? localPoi.Description,
            Localizations = source.Localizations,
            AudioAssets = source.AudioAssets
        };
    }

    private static PoiDto BuildPoiDtoFromLocalPoi(PoiModel localPoi)
    {
        return new PoiDto
        {
            Id = localPoi.Id,
            Title = localPoi.Title,
            Subtitle = localPoi.Subtitle,
            ImageUrl = localPoi.ImageUrl,
            Location = localPoi.Location,
            Latitude = 0,
            Longitude = 0,
            GeofenceRadiusMeters = 100,
            Distance = string.Empty,
            Duration = localPoi.Duration,
            Description = localPoi.Description,
            Provider = localPoi.Provider,
            Credit = localPoi.Credit,
            Category = null,
            PrimaryLanguage = UserProfileService.PreferredLanguage,
            SpeechText = localPoi.SpeechText ?? localPoi.Description,
            Localizations = [],
            AudioAssets = []
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Models;
using TravelApp.Models.Contracts;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public class TourDetailViewModel : INotifyPropertyChanged
{
    private readonly Dictionary<string, string> _speechTextsByLanguage = new(StringComparer.OrdinalIgnoreCase);
    private readonly ObservableCollection<SpeechLanguageOption> _speechLanguages = [];
    private PoiModel? _tour;
    private PoiDto? _currentPoiDto;
    private string _speechTextInput = string.Empty;
    private string _selectedSpeechLanguageCode = string.Empty;
    private bool _isSavingSpeechText;
    private bool _suppressSpeechTextAutoSave;
    private bool _hasPendingSpeechTextChanges;
    private bool _isSpeechLanguageMenuOpen;
    private bool _isBookmarked;
    private bool _canEditSpeechText;
    private int? _currentTourId;
    private CancellationTokenSource? _speechTextAutoSaveCts;
    private readonly IPoiApiClient _poiApiClient;
    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly IAudioLibraryService _audioLibraryService;
    private readonly IBookmarkHistoryService _bookmarkHistoryService;
    private readonly IAudioService _audioService;
    private readonly ITourRouteCatalogService _tourRouteCatalogService;
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

    public bool CanEditSpeechText
    {
        get => _canEditSpeechText;
        private set
        {
            if (_canEditSpeechText == value)
            {
                return;
            }

            _canEditSpeechText = value;
            OnPropertyChanged();
        }
    }

    public bool IsBookmarked
    {
        get => _isBookmarked;
        private set
        {
            if (_isBookmarked == value)
            {
                return;
            }

            _isBookmarked = value;
            OnPropertyChanged();
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
            if (!_suppressSpeechTextAutoSave)
            {
                _hasPendingSpeechTextChanges = true;
            }
            OnPropertyChanged();

            if (!_suppressSpeechTextAutoSave)
            {
                ScheduleSpeechTextAutoSave();
            }
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

    public string ProviderName => Tour?.Provider ?? string.Empty;
    public string Description => Tour?.SpeechText ?? Tour?.Description ?? string.Empty;
    public string Credit => Tour?.Credit ?? string.Empty;
    public string SelectedSpeechLanguageDisplayText => GetLanguageDisplayText(SelectedSpeechLanguageCode);
    public ObservableCollection<SpeechLanguageOption> SpeechLanguages => _speechLanguages;

    public ICommand BackCommand { get; }
    public ICommand PlayAudioCommand { get; }
    public ICommand StopAudioCommand { get; }
    public ICommand SelectLanguageSimpleCommand { get; }
    public ICommand ViewTourCommand { get; }
    public ICommand SaveSpeechTextCommand { get; }
    public ICommand DownloadTourCommand { get; }
    public ICommand ToggleBookmarkCommand { get; }
    public ICommand ToggleSpeechLanguageMenuCommand { get; }
    public ICommand CloseSpeechLanguageMenuCommand { get; }
    public ICommand SelectSpeechLanguageCommand { get; }

    public string SelectedSpeechLanguageCode
    {
        get => _selectedSpeechLanguageCode;
        private set
        {
            var normalized = NormalizeLanguageCode(value);
            if (string.Equals(_selectedSpeechLanguageCode, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _selectedSpeechLanguageCode = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedSpeechLanguageDisplayText));
            // When selected language changes, update displayed title/description and audio selection
            ApplyLocalizationForSelectedLanguage();
        }
    }

    private bool _isPlaying;
    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying == value) return;
            _isPlaying = value;
            OnPropertyChanged();
        }
    }

    private double _playerProgress;
    public double PlayerProgress
    {
        get => _playerProgress;
        set
        {
            if (Math.Abs(_playerProgress - value) < 0.001) return;
            _playerProgress = value;
            OnPropertyChanged();
        }
    }

    private async Task ToggleBookmarkAsync()
    {
        if (Tour is null)
        {
            return;
        }

        try
        {
            await _bookmarkHistoryService.ToggleBookmarkAsync(Tour, CancellationToken.None);
            IsBookmarked = await _bookmarkHistoryService.IsBookmarkedAsync(Tour.Id, CancellationToken.None);
            await Shell.Current.DisplayAlert("Bookmarks", IsBookmarked ? "Tour đã được lưu vào bookmarks." : "Tour đã được xóa khỏi bookmarks.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Không thể cập nhật bookmark: {ex.Message}", "OK");
        }
    }

    public bool IsSpeechLanguageMenuOpen
    {
        get => _isSpeechLanguageMenuOpen;
        private set
        {
            if (_isSpeechLanguageMenuOpen == value)
            {
                return;
            }

            _isSpeechLanguageMenuOpen = value;
            OnPropertyChanged();
        }
    }

    public TourDetailViewModel(ITourRouteCatalogService tourRouteCatalogService, IPoiApiClient poiApiClient, ILocalDatabaseService localDatabaseService, IAudioLibraryService audioLibraryService, IBookmarkHistoryService bookmarkHistoryService, TravelApp.Services.Runtime.TourRouteCacheService tourRouteCacheService, IAudioService audioService)
    {
        _tourRouteCatalogService = tourRouteCatalogService;
        _poiApiClient = poiApiClient;
        _localDatabaseService = localDatabaseService;
        _audioLibraryService = audioLibraryService;
        _bookmarkHistoryService = bookmarkHistoryService;
        _audioService = audioService;
        _tourRouteCacheService = tourRouteCacheService;
        _audioService = audioService;
        _audioService.PlaybackEnded += (_, _) =>
        {
            IsPlaying = false;
            PlayerProgress = 0;
        };
        BackCommand = new Command(async () =>
        {
            await StopAsync();
            await Shell.Current.GoToAsync("..");
        });
        ViewTourCommand = new Command(async () =>
        {
            if (Tour is null)
            {
                return;
            }

            await SaveSpeechTextAsync(showConfirmation: false);
            await Shell.Current.GoToAsync($"TourMapRoutePage?tourId={Tour.Id}&poiId={Tour.Id}&lang={Uri.EscapeDataString(SelectedSpeechLanguageCode)}");
        });
        SaveSpeechTextCommand = new Command(async () => await SaveSpeechTextAsync());
        DownloadTourCommand = new Command(async () => await DownloadTourAsync());
        ToggleBookmarkCommand = new Command(async () => await ToggleBookmarkAsync());
        ToggleSpeechLanguageMenuCommand = new Command(() => IsSpeechLanguageMenuOpen = !IsSpeechLanguageMenuOpen);
        CloseSpeechLanguageMenuCommand = new Command(() => IsSpeechLanguageMenuOpen = false);
        SelectSpeechLanguageCommand = new Command<SpeechLanguageOption>(async option => await SelectSpeechLanguageAsync(option));
        SelectLanguageSimpleCommand = new Command<string>(async lang =>
        {
            if (string.IsNullOrWhiteSpace(lang)) return;
            SelectedSpeechLanguageCode = NormalizeLanguageCode(lang);
            // stop any playing audio
            await _audioService.StopAsync();
        });

        PlayAudioCommand = new Command(async () => await PlayAudioAsync());
        StopAudioCommand = new Command(async () => await _audioService.StopAsync());

        UpdateSpeechTextPermission();
        UserProfileService.ProfileChanged += OnUserProfileChanged;
    }

    private async Task DownloadTourAsync()
    {
        if (Tour is null)
        {
            return;
        }

        try
        {
            var downloaded = await _audioLibraryService.DownloadAsync(Tour.Id, SelectedSpeechLanguageCode, CancellationToken.None);
            var message = downloaded
                ? $"Tour '{Tour.Title}' đã được thêm vào mục download."
                : $"Tour '{Tour.Title}' đã có trong download hoặc đang chờ tải.";

            await Shell.Current.DisplayAlert("Download tour", message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Không thể download tour: {ex.Message}", "OK");
        }
    }

    public Task PersistSpeechTextAsync()
    {
        if (!_hasPendingSpeechTextChanges)
        {
            return Task.CompletedTask;
        }

        CancelSpeechTextAutoSave();
        return SaveSpeechTextAsync(showConfirmation: false);
    }

    public async Task StopAsync()
    {
        if (!_hasPendingSpeechTextChanges)
        {
            return;
        }

        CancelSpeechTextAutoSave();
        await SaveSpeechTextAsync(showConfirmation: false);
    }

    public void Load(string? tourId)
    {
        if (!int.TryParse(tourId, out var id))
            return;

        _currentTourId = id;
        _ = LoadAsync(id);
    }

    private bool _isProcessingAudio; // Thêm biến private này ở đầu class

    private async Task PlayAudioAsync()
    {
        if (_currentPoiDto is null || _isProcessingAudio) return; // Nếu đang xử lý thì thoát luôn

        _isProcessingAudio = true;
        try
        {
            // Luôn dừng audio cũ trước khi phát bất cứ thứ gì mới
            await _audioService.StopAsync();

            IsPlaying = true;
            // Gọi phát audio
            await _audioService.PlayPoiAudioAsync(MapToPoiMobileDto(_currentPoiDto));
        }
        catch (Exception ex)
        {
            IsPlaying = false;
            System.Diagnostics.Debug.WriteLine($"Audio Error: {ex.Message}");
        }
        finally
        {
            _isProcessingAudio = false; // Xử lý xong, mở khóa cho lần nhấn tiếp theo
        }
    }

    private PoiMobileDto MapToPoiMobileDto(PoiDto dto)
    {
        return new PoiMobileDto
        {
            Id = dto.Id,
            Title = dto.Title,
            Subtitle = dto.Subtitle,
            Description = dto.Description,
            LanguageCode = dto.PrimaryLanguage,
            PrimaryLanguage = dto.PrimaryLanguage,
            ImageUrl = dto.ImageUrl,
            Location = dto.Location,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            GeofenceRadiusMeters = dto.GeofenceRadiusMeters ?? 100,
            AudioAssets = dto.AudioAssets.Select(a => new PoiAudioMobileDto { LanguageCode = a.LanguageCode, AudioUrl = a.AudioUrl, Transcript = a.Transcript, IsGenerated = a.IsGenerated }).ToList(),
            SpeechTexts = dto.SpeechTexts.Select(s => new PoiSpeechTextMobileDto { LanguageCode = s.LanguageCode, Text = s.Text }).ToList(),
            SpeechText = dto.SpeechText,
            SpeechTextLanguageCode = dto.SpeechTextLanguageCode
        };
    }

    public Task RefreshAsync()
    {
        if (!_currentTourId.HasValue)
        {
            return Task.CompletedTask;
        }

        var selectedLanguage = SelectedSpeechLanguageCode;
        return RefreshAndRestoreSelectionAsync(_currentTourId.Value, selectedLanguage);
    }

    private async Task RefreshAndRestoreSelectionAsync(int tourId, string? selectedLanguage)
    {
        await LoadAsync(tourId);

        var normalized = NormalizeLanguageCode(selectedLanguage);
        if (!string.IsNullOrWhiteSpace(normalized) && _speechTextsByLanguage.ContainsKey(normalized))
        {
            SelectedSpeechLanguageCode = normalized;
            UpdateSelectedLanguageFlags();
            ApplySpeechTextForSelectedLanguage();
        }
    }

    private async Task LoadAsync(int id)
    {
        _suppressSpeechTextAutoSave = true;
        try
        {
            try
            {
                var dto = await _tourRouteCatalogService.ResolvePoiAsync(id, UserProfileService.PreferredLanguage);
                if (dto is not null)
                {
                    _currentPoiDto = dto;
                    Tour = MapPoi(dto);
                    // Ensure QR image is generated immediately after data loads so mobile QR displays link to public web detail
                    EnsureQrImage(dto);
                    SetLoadedSpeechTexts(dto.SpeechTexts, dto.SpeechTextLanguageCode, dto.SpeechText ?? dto.Description, dto.PrimaryLanguage);
                    _hasPendingSpeechTextChanges = false;
                    IsBookmarked = await _bookmarkHistoryService.IsBookmarkedAsync(id, CancellationToken.None);
                    return;
                }
            }
            catch
            {
            }

            PoiMobileDto? cachedPoi = null;
            try
            {
                var localPois = await _localDatabaseService.GetPoisAsync(UserProfileService.PreferredLanguage, cancellationToken: CancellationToken.None);
                cachedPoi = localPois.FirstOrDefault(x => x.Id == id);
            }
            catch
            {
            }

            _currentPoiDto = null;
            if (cachedPoi is not null)
            {
                var cachedModel = new PoiModel
                {
                    Id = cachedPoi.Id,
                    Title = cachedPoi.Title,
                    Subtitle = cachedPoi.Subtitle,
                    ImageUrl = cachedPoi.ImageUrl,
                    Location = cachedPoi.Location,
                    Distance = string.Empty,
                    Duration = string.Empty,
                    Description = cachedPoi.Description,
                    Provider = null,
                    Credit = null,
                    SpeechText = cachedPoi.SpeechText
                };

                _currentPoiDto = new PoiDto
                {
                    Id = cachedPoi.Id,
                    Title = cachedPoi.Title,
                    Subtitle = cachedPoi.Subtitle,
                    ImageUrl = cachedPoi.ImageUrl,
                    Location = cachedPoi.Location,
                    Latitude = cachedPoi.Latitude,
                    Longitude = cachedPoi.Longitude,
                    GeofenceRadiusMeters = cachedPoi.GeofenceRadiusMeters,
                    Distance = string.Empty,
                    Duration = string.Empty,
                    Description = cachedPoi.Description,
                    Provider = null,
                    Credit = null,
                    Category = cachedPoi.Category,
                    PrimaryLanguage = cachedPoi.PrimaryLanguage,
                    SpeechText = cachedPoi.SpeechText,
                    SpeechTextLanguageCode = cachedPoi.SpeechTextLanguageCode,
                    Localizations = [],
                    AudioAssets = cachedPoi.AudioAssets.Select(audio => new PoiAudioDto(audio.LanguageCode, audio.AudioUrl, audio.Transcript, audio.IsGenerated)).ToList(),
                    SpeechTexts = cachedPoi.SpeechTexts.Select(x => new PoiSpeechTextDto(x.LanguageCode, x.Text)).ToList()
                };

                Tour = cachedModel;
                EnsureQrImage(_currentPoiDto);
                SetLoadedSpeechTexts(_currentPoiDto.SpeechTexts, _currentPoiDto.SpeechTextLanguageCode, _currentPoiDto.SpeechText ?? _currentPoiDto.Description, _currentPoiDto.PrimaryLanguage);
                _hasPendingSpeechTextChanges = false;
                IsBookmarked = await _bookmarkHistoryService.IsBookmarkedAsync(id, CancellationToken.None);
                return;
            }

            Tour = null;
            SpeechTextInput = string.Empty;
            _hasPendingSpeechTextChanges = false;
            IsBookmarked = false;
        }
        finally
        {
            _suppressSpeechTextAutoSave = false;
        }
    }

    private async Task SaveSpeechTextAsync(bool showConfirmation = true)
    {
        if (!CanEditSpeechText)
        {
            return;
        }

        if (Tour is null || _currentPoiDto is null || IsSavingSpeechText)
        {
            return;
        }

        IsSavingSpeechText = true;
        try
        {
            var selectedLanguage = NormalizeLanguageCode(SelectedSpeechLanguageCode);
            var speechText = SpeechTextInput?.Trim();
            if (!string.IsNullOrWhiteSpace(selectedLanguage))
            {
                _speechTextsByLanguage[selectedLanguage] = speechText ?? string.Empty;
            }

            var speechTexts = _speechTextsByLanguage
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => new PoiSpeechTextDto(x.Key, x.Value.Trim()))
                .OrderBy(x => x.LanguageCode, StringComparer.OrdinalIgnoreCase)
                .ToList();

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
                selectedLanguage,
                _currentPoiDto.Localizations,
                _currentPoiDto.AudioAssets,
                speechTexts);

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
                    SpeechTextLanguageCode = selectedLanguage,
                    AudioAssets = _currentPoiDto.AudioAssets.Select(audio => new PoiAudioMobileDto
                    {
                        LanguageCode = audio.LanguageCode,
                        AudioUrl = audio.AudioUrl,
                        Transcript = audio.Transcript,
                        IsGenerated = audio.IsGenerated
                    }).ToList(),
                    SpeechTexts = speechTexts.Select(x => new PoiSpeechTextMobileDto { LanguageCode = x.LanguageCode, Text = x.Text }).ToList()
                }
            ], CancellationToken.None);

            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                await _poiApiClient.UpdateAsync(_currentPoiDto.Id, request);
            }

            await _tourRouteCacheService.InvalidateAsync(_currentPoiDto.Id, null, CancellationToken.None);

            _suppressSpeechTextAutoSave = true;
            _currentPoiDto.SpeechText = speechText;
            _currentPoiDto.SpeechTextLanguageCode = selectedLanguage;
            _currentPoiDto.SpeechTexts = speechTexts;
            Tour.SpeechText = string.IsNullOrWhiteSpace(speechText) ? null : speechText;
            OnPropertyChanged(nameof(Tour));
            OnPropertyChanged(nameof(Description));
            SpeechTextInput = speechText ?? string.Empty;
            _hasPendingSpeechTextChanges = false;
            _suppressSpeechTextAutoSave = false;
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

    private void ScheduleSpeechTextAutoSave()
    {
        CancelSpeechTextAutoSave();
        _speechTextAutoSaveCts = new CancellationTokenSource();

        var token = _speechTextAutoSaveCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(800, token);
                await SaveSpeechTextAsync(showConfirmation: false);
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }

    private void CancelSpeechTextAutoSave()
    {
        if (_speechTextAutoSaveCts is null)
        {
            return;
        }

        _speechTextAutoSaveCts.Cancel();
        _speechTextAutoSaveCts.Dispose();
        _speechTextAutoSaveCts = null;
    }

    private async Task SelectSpeechLanguageAsync(SpeechLanguageOption? option)
    {
        if (option is null || !CanEditSpeechText)
        {
            return;
        }

        IsSpeechLanguageMenuOpen = false;
        if (_hasPendingSpeechTextChanges)
        {
            await PersistSpeechTextAsync();
        }
        else if (_currentTourId.HasValue)
        {
            await RefreshAsync();
        }

        SelectedSpeechLanguageCode = NormalizeLanguageCode(option.LanguageCode);
        UpdateSelectedLanguageFlags();
        ApplySpeechTextForSelectedLanguage();
        ApplyLocalizationForSelectedLanguage();
    }

    private void ApplyLocalizationForSelectedLanguage()
    {
        if (_currentPoiDto is null || Tour is null) return;

        var lang = SelectedSpeechLanguageCode;

        // Update title/description from Localizations
        var loc = _currentPoiDto.Localizations?.FirstOrDefault(l => string.Equals(l.LanguageCode, lang, StringComparison.OrdinalIgnoreCase));
        if (loc is not null)
        {
            Tour.Title = string.IsNullOrWhiteSpace(loc.Title) ? Tour.Title : loc.Title;
            Tour.Subtitle = string.IsNullOrWhiteSpace(loc.Subtitle) ? Tour.Subtitle : loc.Subtitle ?? string.Empty;
            Tour.Description = string.IsNullOrWhiteSpace(loc.Description) ? Tour.Description : loc.Description ?? string.Empty;
            OnPropertyChanged(nameof(Tour));
            OnPropertyChanged(nameof(Description));
        }
    }

    private void SetLoadedSpeechTexts(IReadOnlyList<PoiSpeechTextDto> speechTexts, string? selectedLanguageHint, string? fallbackText, string? primaryLanguage)
    {
        _speechTextsByLanguage.Clear();

        foreach (var speechText in speechTexts)
        {
            var languageCode = NormalizeLanguageCode(speechText.LanguageCode);
            if (string.IsNullOrWhiteSpace(languageCode) || string.IsNullOrWhiteSpace(speechText.Text))
            {
                continue;
            }

            _speechTextsByLanguage[languageCode] = speechText.Text.Trim();
        }

        if (_speechTextsByLanguage.Count == 0 && !string.IsNullOrWhiteSpace(fallbackText))
        {
            var defaultLanguage = NormalizeLanguageCode(selectedLanguageHint ?? primaryLanguage);
            _speechTextsByLanguage[defaultLanguage] = fallbackText.Trim();
        }

        var persistedLanguage = NormalizeLanguageCode(selectedLanguageHint ?? primaryLanguage);
        SelectedSpeechLanguageCode = !string.IsNullOrWhiteSpace(persistedLanguage) && _speechTextsByLanguage.ContainsKey(persistedLanguage)
            ? persistedLanguage
            : NormalizeLanguageCode(_speechTextsByLanguage.Keys.FirstOrDefault());

        UpdateSelectedLanguageFlags();
        ApplySpeechTextForSelectedLanguage();
        _ = RefreshSpeechLanguagesAsync();
    }

    private void ApplySpeechTextForSelectedLanguage()
    {
        var text = GetSpeechTextForLanguage(SelectedSpeechLanguageCode);

        _suppressSpeechTextAutoSave = true;
        SpeechTextInput = text;
        _suppressSpeechTextAutoSave = false;

        if (Tour is not null)
        {
            Tour.SpeechText = string.IsNullOrWhiteSpace(text) ? null : text;
            OnPropertyChanged(nameof(Tour));
            OnPropertyChanged(nameof(Description));
        }
    }

    private string GetSpeechTextForLanguage(string languageCode)
    {
        var normalized = NormalizeLanguageCode(languageCode);
        return _speechTextsByLanguage.TryGetValue(normalized, out var text)
            ? text
            : string.Empty;
    }

    private void UpdateSelectedLanguageFlags()
    {
        foreach (var language in _speechLanguages)
        {
            language.IsSelected = string.Equals(language.LanguageCode, SelectedSpeechLanguageCode, StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task RefreshSpeechLanguagesAsync()
    {
        if (_speechLanguages.Count > 0)
        {
            UpdateSelectedLanguageFlags();
            return;
        }

        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var items = new List<SpeechLanguageOption>();

            foreach (var code in _speechTextsByLanguage.Keys.Concat([SelectedSpeechLanguageCode, UserProfileService.PreferredLanguage]))
            {
                AddLanguageCode(code, items, codes);
            }

            foreach (var locale in locales)
            {
                AddLanguageCode(locale.Language, items, codes);
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _speechLanguages.Clear();
                foreach (var item in items.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
                {
                    item.IsSelected = string.Equals(item.LanguageCode, SelectedSpeechLanguageCode, StringComparison.OrdinalIgnoreCase);
                    _speechLanguages.Add(item);
                }
            });
        }
        catch
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _speechLanguages.Clear();
                foreach (var code in _speechTextsByLanguage.Keys.Concat([SelectedSpeechLanguageCode]).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    _speechLanguages.Add(new SpeechLanguageOption
                    {
                        LanguageCode = NormalizeLanguageCode(code),
                        DisplayName = GetLanguageDisplayText(code),
                        IsSelected = string.Equals(NormalizeLanguageCode(code), SelectedSpeechLanguageCode, StringComparison.OrdinalIgnoreCase)
                    });
                }
            });
        }
    }

    private static void AddLanguageCode(string? languageCode, ICollection<SpeechLanguageOption> items, ISet<string> codes)
    {
        var normalized = NormalizeLanguageCode(languageCode);
        if (string.IsNullOrWhiteSpace(normalized) || !codes.Add(normalized))
        {
            return;
        }

        items.Add(new SpeechLanguageOption
        {
            LanguageCode = normalized,
            DisplayName = GetLanguageDisplayText(normalized)
        });
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? string.Empty
            : languageCode.Trim().ToLowerInvariant();
    }

    private static string GetLanguageDisplayText(string? languageCode)
    {
        var normalized = NormalizeLanguageCode(languageCode);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "--";
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(normalized);
            return string.IsNullOrWhiteSpace(culture.NativeName)
                ? normalized.ToUpperInvariant()
                : $"{culture.NativeName} ({normalized.ToUpperInvariant()})";
        }
        catch
        {
            return normalized.ToUpperInvariant();
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

    // Generate QR image url pointing to admin public detail using AppConfig if available
    private void EnsureQrImage(PoiDto dto)
    {
        try
        {
            var config = MauiProgram.Services.GetRequiredService<AppConfig>();
            // Build public web details URL for mobile QR scans
            var host = (config.AdminHost?.TrimEnd('/') ?? "http://192.168.100.164");
            var portPart = config.AdminPort > 0 ? ":" + config.AdminPort.ToString() : string.Empty;
            var qrLink = $"{host}{portPart}/public/poi/detail/{dto.Id}";
            var qrUrl = config.QuickChartQrBase + System.Uri.EscapeDataString(qrLink);
            if (Tour is not null)
            {
                Tour.QrImageUrl = qrUrl;
                OnPropertyChanged(nameof(Tour));
            }
        }
        catch
        {
        }
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
            AudioAssets = source.AudioAssets,
            SpeechTextLanguageCode = localPoi.SpeechText is not null ? source.SpeechTextLanguageCode : source.SpeechTextLanguageCode,
            SpeechTexts = source.SpeechTexts
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
            AudioAssets = [],
            SpeechTextLanguageCode = "vi",
            SpeechTexts = [new PoiSpeechTextDto("vi", localPoi.SpeechText ?? localPoi.Description ?? string.Empty)]
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnUserProfileChanged(object? sender, EventArgs e)
    {
        UpdateSpeechTextPermission();
    }

    private void UpdateSpeechTextPermission()
    {
        CanEditSpeechText = UserProfileService.CanEditSpeechText;
    }

    public void Dispose()
    {
        UserProfileService.ProfileChanged -= OnUserProfileChanged;
    }
}

public sealed class SpeechLanguageOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public string LanguageCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

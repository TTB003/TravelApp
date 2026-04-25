using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Models;
using TravelApp.Mobile.Services;
using TravelApp.Models.Contracts;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

[QueryProperty(nameof(Autoplay), "autoplay")]
public class TourDetailViewModel : INotifyPropertyChanged
{
    private readonly Dictionary<string, string> _speechTextsByLanguage = new(StringComparer.OrdinalIgnoreCase);
    private readonly ObservableCollection<SpeechLanguageOption> _speechLanguages = [];
    private PoiModel? _tour;
    private PoiDto? _currentPoiDto;
    private string _speechTextInput = string.Empty;
    private string _currentPlayingText = string.Empty;
    private string _selectedSpeechLanguageCode = string.Empty;
    private bool _isSavingSpeechText;
    private bool _suppressSpeechTextAutoSave;
    private bool _hasPendingSpeechTextChanges;
    private bool _isSpeechLanguageMenuOpen;
    private bool _isBookmarked;
    private bool _canEditSpeechText;
    private bool _hasAutoPlayed;

    // Lưu trữ lịch sử phát tự động để tránh phát lại quá gần nhau (Static để tồn tại suốt phiên làm việc của App)
    public static readonly Dictionary<int, DateTime> AutoPlayHistory = new();
    public static readonly TimeSpan AutoPlayCooldown = TimeSpan.FromMinutes(20); // Giới hạn 20 phút cho mỗi POI

    public static bool IsInCooldown(int poiId) => 
        AutoPlayHistory.TryGetValue(poiId, out var lastPlayed) && 
        (DateTime.UtcNow - lastPlayed) < AutoPlayCooldown;

    private CancellationTokenSource? _locationTrackingCts;
    private int? _currentTourId;
    private bool _autoplayRequested;
    private CancellationTokenSource? _speechTextAutoSaveCts;
    private readonly IPoiApiClient _poiApiClient;
    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly IAudioLibraryService _audioLibraryService;
    private readonly IBookmarkHistoryService _bookmarkHistoryService;
    private readonly IAudioService _audioService;
    private readonly ITourRouteCatalogService _tourRouteCatalogService;
    private readonly TravelApp.Services.Runtime.TourRouteCacheService _tourRouteCacheService;

    public string Autoplay
    {
        set => _autoplayRequested = value == "true";
    }

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

            // Cập nhật văn bản hiển thị ngay lập tức nếu đang phát TTS
            // (Nếu văn bản đang phát khớp với văn bản cũ trong editor, ta cập nhật nó theo nội dung mới)
            if (IsPlaying && string.Equals(CurrentPlayingText, _speechTextInput, StringComparison.OrdinalIgnoreCase))
            {
                CurrentPlayingText = value;
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

    public string CurrentPlayingText
    {
        get => _currentPlayingText;
        private set
        {
            if (_currentPlayingText == value) return;
            _currentPlayingText = value;
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
            ApplySpeechTextForSelectedLanguage();
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
            StopLocationTracking();
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
            var normalized = NormalizeLanguageCode(lang);
            SelectedSpeechLanguageCode = normalized;
            // Cập nhật ngôn ngữ toàn cầu khi người dùng chủ động nhấn vào biểu tượng ngôn ngữ/cờ
            LocalizationManager.Instance.SetLanguage(normalized);
            // stop any playing audio
            await _audioService.StopAsync();
        });

        PlayAudioCommand = new Command(async () => await PlayAudioAsync());
        StopAudioCommand = new Command(async () => await _audioService.StopAsync());

        UpdateSpeechTextPermission();
        UserProfileService.ProfileChanged += OnUserProfileChanged;

        // Lắng nghe thay đổi ngôn ngữ toàn cục để cập nhật TTS ngay lập tức
        LocalizationManager.Instance.PropertyChanged += (s, e) => {
            if (string.IsNullOrEmpty(e.PropertyName)) RefreshLanguageFromGlobal();
        };
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
            await _audioService.StopAsync();

            // Chuẩn bị dữ liệu phát: Ưu tiên nội dung đang hiển thị trên màn hình
            var mobileDto = MapToPoiMobileDto(_currentPoiDto);
            
            // CẬP NHẬT: Gán chính xác văn bản và ngôn ngữ đang chọn để TTS đọc đúng giọng
            mobileDto.SpeechText = SpeechTextInput; 
            mobileDto.SpeechTextLanguageCode = SelectedSpeechLanguageCode;

            // Xác định văn bản hiển thị: Ưu tiên transcript của file audio nếu có, ngược lại dùng SpeechText
            var audioAsset = mobileDto.AudioAssets?.FirstOrDefault(a => string.Equals(a.LanguageCode, SelectedSpeechLanguageCode, StringComparison.OrdinalIgnoreCase));
            if (audioAsset != null && !string.IsNullOrWhiteSpace(audioAsset.Transcript))
            {
                CurrentPlayingText = audioAsset.Transcript;
            }
            else
            {
                CurrentPlayingText = SpeechTextInput;
            }

            IsPlaying = true;
            
            // AudioService sẽ tự động kiểm tra: 
            // 1. Nếu có AudioAsset (file mp3) khớp với SelectedSpeechLanguageCode -> Phát file.
            // 2. Nếu không có file -> Sử dụng TTS để đọc mobileDto.SpeechText bằng giọng mobileDto.SpeechTextLanguageCode.
            await _audioService.PlayPoiAudioAsync(mobileDto);

            // Ghi nhận thời điểm phát vào lịch sử (cả manual và auto) để tính cooldown
            AutoPlayHistory[mobileDto.Id] = DateTime.UtcNow;

            // Ghi nhận lượt nghe Audio về server (không đợi kết quả để tránh làm chậm UI)
            _ = Task.Run(async () => {
                var config = MauiProgram.Services.GetRequiredService<AppConfig>();
                var client = new HttpClient();
                await client.PostAsync($"{config.ApiBaseUrl}api/pois/{mobileDto.Id}/audio-play", null);
            });
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
                    var model = MapPoi(dto);
                    // Đảm bảo QR được gán vào model trước khi gán model vào property Tour để UI nhận đủ data một lần
                    PopulateQrImageUrl(model, dto.Id);
                    Tour = model;
                    
                    SetLoadedSpeechTexts(dto.SpeechTexts, dto.SpeechTextLanguageCode, dto.SpeechText ?? dto.Description, dto.PrimaryLanguage);
                    _ = StartLocationTrackingAsync(_autoplayRequested);
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

                PopulateQrImageUrl(cachedModel, cachedPoi.Id);
                Tour = cachedModel;
                
                SetLoadedSpeechTexts(_currentPoiDto.SpeechTexts, _currentPoiDto.SpeechTextLanguageCode, _currentPoiDto.SpeechText ?? _currentPoiDto.Description, _currentPoiDto.PrimaryLanguage);
                _ = StartLocationTrackingAsync(_autoplayRequested);
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

    private async Task StartLocationTrackingAsync(bool immediateCheck = false)
    {
        if (_currentPoiDto == null || _hasAutoPlayed) return;

        // Kiểm tra giới hạn (Cooldown): Nếu đã phát trong vòng 20 phút qua thì không tự động phát lại
        if (IsInCooldown(_currentPoiDto.Id))
        {
            _hasAutoPlayed = true; 
            return;
        }

        StopLocationTracking();
        _locationTrackingCts = new CancellationTokenSource();
        var token = _locationTrackingCts.Token;

        try
        {
            // Nếu được yêu cầu Autoplay từ trang Explore, kiểm tra vị trí và phát ngay lập tức
            if (immediateCheck)
            {
                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)), token);
                if (location != null && _currentPoiDto != null)
                {
                    double distance = location.CalculateDistance(_currentPoiDto.Latitude, _currentPoiDto.Longitude, DistanceUnits.Kilometers) * 1000;
                    double radius = _currentPoiDto.GeofenceRadiusMeters ?? 100;

                    if (distance <= radius && !IsPlaying)
                    {
                        _hasAutoPlayed = true;
                        _autoplayRequested = false;
                        await MainThread.InvokeOnMainThreadAsync(async () => await PlayAudioAsync());
                        // Vẫn cho vòng lặp chạy tiếp để cập nhật UI nếu cần
                    }
                }
            }

            // Vòng lặp kiểm tra vị trí định kỳ mỗi 10 giây
            while (!token.IsCancellationRequested && !_hasAutoPlayed)
            {
                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)), token);
                if (location != null && _currentPoiDto != null)
                {
                    double distance = location.CalculateDistance(_currentPoiDto.Latitude, _currentPoiDto.Longitude, DistanceUnits.Kilometers) * 1000;
                    
                    // Bán kính kích hoạt (ưu tiên GeofenceRadius của POI, mặc định 100m)
                    double radius = _currentPoiDto.GeofenceRadiusMeters ?? 100;

                    if (distance <= radius && !IsPlaying)
                    {
                        _hasAutoPlayed = true;
                        MainThread.BeginInvokeOnMainThread(async () => await PlayAudioAsync());
                        break; 
                    }
                }
                await Task.Delay(10000, token); // Đợi 10s trước khi check lại để tiết kiệm pin
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Location Tracking Error: {ex.Message}");
        }
    }

    private void StopLocationTracking()
    {
        if (_locationTrackingCts != null)
        {
            _locationTrackingCts.Cancel();
            _locationTrackingCts.Dispose();
            _locationTrackingCts = null;
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

    private void RefreshLanguageFromGlobal()
    {
        var globalLang = LocalizationManager.Instance.CurrentLanguage;
        if (SelectedSpeechLanguageCode != globalLang) SelectedSpeechLanguageCode = globalLang;
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

        var newLang = NormalizeLanguageCode(option.LanguageCode);
        SelectedSpeechLanguageCode = newLang;
        // Đồng bộ toàn hệ thống khi người dùng chọn từ menu danh sách ngôn ngữ
        LocalizationManager.Instance.SetLanguage(newLang);

        UpdateSelectedLanguageFlags();
        ApplySpeechTextForSelectedLanguage();
        ApplyLocalizationForSelectedLanguage();
    }

    private void ApplyLocalizationForSelectedLanguage()
    {
        if (_currentPoiDto is null || Tour is null)
            return;
            
        var lang = SelectedSpeechLanguageCode;
        var currentTour = Tour;

        // 1. Reset về giá trị mặc định (Thường là ngôn ngữ gốc/Tiếng Việt)
        currentTour.Title = _currentPoiDto.Title;
        currentTour.Subtitle = _currentPoiDto.Subtitle ?? string.Empty;
        currentTour.Description = _currentPoiDto.Description ?? string.Empty;
        
        // 2. Kiểm tra nếu POI này hoàn toàn không có bản dịch cho ngôn ngữ đang chọn
        var hasLocalization = _currentPoiDto.Localizations?.Any(l => string.Equals(l.LanguageCode, lang, StringComparison.OrdinalIgnoreCase)) ?? false;
        var hasSpeechText = _speechTextsByLanguage.ContainsKey(NormalizeLanguageCode(lang));

        // Nếu là POI mới (chỉ có tiếng Việt) mà App đang ở tiếng khác, 
        // ta cần ép SelectedSpeechLanguageCode về tiếng Việt để TTS đọc đúng giọng
        if (!hasLocalization && !hasSpeechText)
        {
            var primary = NormalizeLanguageCode(_currentPoiDto.PrimaryLanguage);
            if (!string.IsNullOrEmpty(primary) && primary != lang)
            {
                // Cập nhật ngầm code để TTS sử dụng, nhưng không gọi lại Apply để tránh loop
                _selectedSpeechLanguageCode = primary;
                OnPropertyChanged(nameof(SelectedSpeechLanguageCode));
                OnPropertyChanged(nameof(SelectedSpeechLanguageDisplayText));
                lang = primary;
            }
        }

        // 3. Tìm bản dịch trong danh sách Localizations
        var loc = _currentPoiDto.Localizations?.FirstOrDefault(l => string.Equals(l.LanguageCode, lang, StringComparison.OrdinalIgnoreCase));
        if (loc is not null)
        {
            if (!string.IsNullOrWhiteSpace(loc.Title)) currentTour.Title = loc.Title;
            if (!string.IsNullOrWhiteSpace(loc.Subtitle)) currentTour.Subtitle = loc.Subtitle;
            if (!string.IsNullOrWhiteSpace(loc.Description)) currentTour.Description = loc.Description;
        }
        
        // 4. Cập nhật mã QR và ép UI Refresh bằng cách Notify lại thuộc tính Tour
        PopulateQrImageUrl(currentTour, _currentPoiDto.Id);
        
        // Notify cho Tour cuối cùng để kích hoạt lại toàn bộ binding liên quan trong XAML
        // Thông báo UI: Phải Notify Tour cuối cùng để kích hoạt lại toàn bộ binding trong XAML
        // nhưng cần notify Description trước để các nhãn đơn lẻ cập nhật giá trị mới.
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(Tour));
        OnPropertyChanged(nameof(SelectedSpeechLanguageDisplayText));
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

        // Xác định ngôn ngữ hiển thị ban đầu dựa trên thứ tự ưu tiên để không "hijack" ngôn ngữ của App:
        // 1. Ngôn ngữ hiện tại của App (Nếu POI có speech text tương ứng thì dùng luôn)
        // 2. Ngôn ngữ gợi ý từ Admin (selectedLanguageHint/primaryLanguage)
        // 3. Ngôn ngữ đầu tiên có sẵn trong danh sách SpeechTexts
        var currentAppLang = LocalizationManager.Instance.CurrentLanguage;
        var poiHintLang = NormalizeLanguageCode(selectedLanguageHint ?? primaryLanguage);

        string targetLang = currentAppLang;
        if (!_speechTextsByLanguage.ContainsKey(targetLang))
        {
            if (!string.IsNullOrWhiteSpace(poiHintLang) && _speechTextsByLanguage.ContainsKey(poiHintLang))
                targetLang = poiHintLang;
            else
                targetLang = NormalizeLanguageCode(_speechTextsByLanguage.Keys.FirstOrDefault()) ?? currentAppLang;
        }

        SelectedSpeechLanguageCode = targetLang;

        UpdateSelectedLanguageFlags();
        ApplySpeechTextForSelectedLanguage();
        _ = RefreshSpeechLanguagesAsync();
    }

    private void ApplySpeechTextForSelectedLanguage()
    {
        var targetLang = SelectedSpeechLanguageCode;
        var text = GetSpeechTextForLanguage(targetLang);

        // Fallback cho SpeechText: Nếu ngôn ngữ chọn không có text, lấy text của ngôn ngữ gốc
        if (string.IsNullOrWhiteSpace(text) && _currentPoiDto != null)
        {
            var primary = NormalizeLanguageCode(_currentPoiDto.PrimaryLanguage);
            if (!string.IsNullOrEmpty(primary) && primary != targetLang)
            {
                text = GetSpeechTextForLanguage(primary);
            }
        }

        _suppressSpeechTextAutoSave = true;
        SpeechTextInput = text;
        _suppressSpeechTextAutoSave = false;

        if (IsPlaying)
        {
            CurrentPlayingText = text;
        }

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
        try
        {
            // Chỉ hiển thị 4 ngôn ngữ chính mà App đang hỗ trợ theo yêu cầu
            var supportedAppLanguages = new[] { "vi", "en", "fr", "ja" };
            var items = supportedAppLanguages.Select(code => new SpeechLanguageOption
            {
                LanguageCode = code,
                DisplayName = GetLanguageDisplayText(code),
                IsSelected = string.Equals(code, SelectedSpeechLanguageCode, StringComparison.OrdinalIgnoreCase)
            }).ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _speechLanguages.Clear();
                foreach (var item in items) _speechLanguages.Add(item);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing speech languages: {ex.Message}");
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
            // Đảm bảo "ja-JP" hay "ja_JP" đều về "ja" để khớp với Admin
            : languageCode.Trim().Split('-')[0].Split('_')[0].ToLowerInvariant();
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

    /// <summary>
    /// Logic tạo URL QR Code hướng về trang web công khai của POI.
    /// Tách riêng logic gán URL để có thể gọi linh hoạt.
    /// </summary>
    private void PopulateQrImageUrl(PoiModel model, int poiId)
    {
        try
        {
            var config = MauiProgram.Services.GetRequiredService<AppConfig>();
            var apiBase = config.ApiBaseUrl?.TrimEnd('/') ?? "http://192.168.100.164:5001";

            // Tự động lấy Host từ apiBase nếu không cấu hình AdminHost để đồng bộ IP
            var apiUri = new Uri(apiBase);
            var hostIp = apiUri.Host;

            // Fix: Nếu host là 10.0.2.2 (Android Emulator), chuyển về IP thật để QR có thể quét được từ ngoài
            if (hostIp == "10.0.2.2") hostIp = "192.168.100.164";

            var host = config.AdminHost?.TrimEnd('/') ?? $"{apiUri.Scheme}://{hostIp}";

            // Sử dụng Port 7020 của Admin Web
            var port = config.PublicWebPort > 0 ? config.PublicWebPort : 7020;
            var portPart = $":{port}";
            
            // Trỏ qua API tracking để đếm lượt quét trước khi redirect về Web UI
            var redirectUrl = $"{host}{portPart}/Public/Details/{poiId}";
            
            // Nội dung mã QR cũng cần dùng IP thật thay vì IP giả lập 10.0.2.2
            var qrApiBase = apiBase.Replace("10.0.2.2", hostIp);
            var qrLink = $"{qrApiBase}/api/pois/{poiId}/qr-track?redirectUrl={System.Uri.EscapeDataString(redirectUrl)}";
            var qrUrl = config.QuickChartQrBase + System.Uri.EscapeDataString(qrLink);
            
            model.QrImageUrl = qrUrl;
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"QR Generation Error: {ex.Message}"); }
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
        // Kiểm tra nếu người dùng là Owner thì cho phép chỉnh sửa Speech Text
        var isOwner = UserProfileService.Roles?.Contains("Owner", StringComparer.OrdinalIgnoreCase) ?? false;
        CanEditSpeechText = isOwner || UserProfileService.CanEditSpeechText;
    }

    public void Dispose()
    {
        StopLocationTracking();
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

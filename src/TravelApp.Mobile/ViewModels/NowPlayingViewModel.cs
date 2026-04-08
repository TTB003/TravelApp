using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public sealed class NowPlayingViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IAudioPlayerService _audioPlayerService;

    private bool _isPlaying;
    private string _poiTitle = "Chưa phát audio";
    private string _languageCode = string.Empty;

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (_isPlaying == value)
            {
                return;
            }

            _isPlaying = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(ActionButtonText));
        }
    }

    public string PoiTitle
    {
        get => _poiTitle;
        private set
        {
            if (_poiTitle == value)
            {
                return;
            }

            _poiTitle = value;
            OnPropertyChanged();
        }
    }

    public string LanguageCode
    {
        get => _languageCode;
        private set
        {
            if (_languageCode == value)
            {
                return;
            }

            _languageCode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LanguageText));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public string LanguageText => string.IsNullOrWhiteSpace(LanguageCode) ? "Ngôn ngữ: --" : $"Ngôn ngữ: {LanguageCode}";

    public string StatusText => IsPlaying ? $"Đang phát • {LanguageText}" : "Đã dừng";

    public string ActionButtonText => IsPlaying ? "Stop" : "Back";

    public ICommand BackCommand { get; }
    public ICommand ActionCommand { get; }

    public NowPlayingViewModel(IAudioPlayerService audioPlayerService)
    {
        _audioPlayerService = audioPlayerService;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        ActionCommand = new Command(async () =>
        {
            if (IsPlaying)
            {
                await _audioPlayerService.StopAsync();
                return;
            }

            await Shell.Current.GoToAsync("..");
        });

        _audioPlayerService.PlaybackStateChanged += OnPlaybackStateChanged;
        ApplyState(_audioPlayerService.IsPlaying, _audioPlayerService.CurrentPoiTitle, _audioPlayerService.CurrentLanguageCode);
    }

    private void OnPlaybackStateChanged(object? sender, AudioPlaybackStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => ApplyState(e.IsPlaying, e.PoiTitle, _audioPlayerService.CurrentLanguageCode));
    }

    private void ApplyState(bool isPlaying, string? poiTitle, string? languageCode)
    {
        IsPlaying = isPlaying;
        PoiTitle = isPlaying ? (string.IsNullOrWhiteSpace(poiTitle) ? "Địa điểm hiện tại" : poiTitle) : "Chưa phát audio";
        LanguageCode = isPlaying ? (string.IsNullOrWhiteSpace(languageCode) ? "--" : languageCode) : string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _audioPlayerService.PlaybackStateChanged -= OnPlaybackStateChanged;
    }
}

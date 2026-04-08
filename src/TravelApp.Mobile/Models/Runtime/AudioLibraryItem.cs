using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TravelApp.Models.Runtime;

public sealed class AudioLibraryItem : INotifyPropertyChanged
{
    private int _poiId;
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private string _location = string.Empty;
    private string _imageUrl = string.Empty;
    private string _languageCode = "en";
    private string? _audioUrl;
    private bool _isDownloaded;
    private string? _localFilePath;
    private long _fileSizeBytes;
    private bool _isBusy;
    private double _downloadProgress;
    private string _downloadStatusText = string.Empty;
    private bool _isPlaying;

    public int PoiId { get => _poiId; set => SetField(ref _poiId, value); }
    public string Title { get => _title; set => SetField(ref _title, value); }
    public string Subtitle { get => _subtitle; set => SetField(ref _subtitle, value); }
    public string Location { get => _location; set => SetField(ref _location, value); }
    public string ImageUrl { get => _imageUrl; set => SetField(ref _imageUrl, value); }
    public string LanguageCode { get => _languageCode; set => SetField(ref _languageCode, value); }
    public string? AudioUrl { get => _audioUrl; set => SetField(ref _audioUrl, value); }
    public bool IsDownloaded { get => _isDownloaded; set => SetField(ref _isDownloaded, value); }
    public string? LocalFilePath { get => _localFilePath; set => SetField(ref _localFilePath, value); }
    public long FileSizeBytes { get => _fileSizeBytes; set => SetField(ref _fileSizeBytes, value); }
    public bool IsBusy { get => _isBusy; set => SetField(ref _isBusy, value); }
    public double DownloadProgress { get => _downloadProgress; set => SetField(ref _downloadProgress, value); }
    public string DownloadStatusText { get => _downloadStatusText; set => SetField(ref _downloadStatusText, value); }
    public bool IsPlaying { get => _isPlaying; set => SetField(ref _isPlaying, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TravelApp.Services.Abstractions;
using TravelApp.ViewModels;

namespace TravelApp;

public partial class ExplorePage : ContentPage
{
    private readonly ITravelBootstrapService _travelBootstrapService;
    private readonly IAudioPlayerService _audioPlayerService;
    private readonly ILogger<ExplorePage> _logger;
    private IDispatcherTimer? _audioStatusTimer;

    public ExplorePage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<ExploreViewModel>();
        _travelBootstrapService = MauiProgram.Services.GetRequiredService<ITravelBootstrapService>();
        _audioPlayerService = MauiProgram.Services.GetRequiredService<IAudioPlayerService>();
        _logger = MauiProgram.Services.GetRequiredService<ILogger<ExplorePage>>();
        InitializeMap();
        UpdateAudioStatus();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartAudioStatusTimer();
        _ = StartRuntimeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopAudioStatusTimer();
        _ = StopRuntimeAsync();
    }

    private void InitializeMap()
    {
        var hoChiMinhCity = new Location(10.7769, 106.7009);

        ExploreMap.MoveToRegion(MapSpan.FromCenterAndRadius(
            hoChiMinhCity,
            Distance.FromKilometers(1)));

        ExploreMap.Pins.Add(new Pin
        {
            Label = "Ho Chi Minh City",
            Address = "District 1",
            Type = PinType.Place,
            Location = hoChiMinhCity
        });
    }

    private async Task StartRuntimeAsync()
    {
        try
        {
            await _travelBootstrapService.StartAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Explore page: failed to start runtime pipeline.");
        }
    }

    private async Task StopRuntimeAsync()
    {
        try
        {
            await _travelBootstrapService.StopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Explore page: failed to stop runtime pipeline.");
        }
    }

    private void StartAudioStatusTimer()
    {
        if (_audioStatusTimer is not null)
        {
            _audioStatusTimer.Start();
            return;
        }

        _audioStatusTimer = Dispatcher.CreateTimer();
        _audioStatusTimer.Interval = TimeSpan.FromMilliseconds(500);
        _audioStatusTimer.Tick += OnAudioStatusTick;
        _audioStatusTimer.Start();
    }

    private void StopAudioStatusTimer()
    {
        if (_audioStatusTimer is null)
        {
            return;
        }

        _audioStatusTimer.Stop();
        _audioStatusTimer.Tick -= OnAudioStatusTick;
        _audioStatusTimer = null;

        AudioStatusBorder.IsVisible = false;
    }

    private void OnAudioStatusTick(object? sender, EventArgs e)
    {
        UpdateAudioStatus();
    }

    private void UpdateAudioStatus()
    {
        var isPlaying = _audioPlayerService.IsPlaying;
        AudioStatusBorder.IsVisible = isPlaying;

        if (!isPlaying)
        {
            AudioStatusTextLabel.Text = "";
            AudioPoiTitleLabel.Text = "";
            return;
        }

        AudioStatusTextLabel.Text = "Đang phát audio";
        AudioPoiTitleLabel.Text = _audioPlayerService.CurrentPoiTitle ?? "Địa điểm hiện tại";
    }
}

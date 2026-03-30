using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using TravelApp.Services.Abstractions;

namespace TravelApp;

public partial class DebugRuntimeConsolePage : ContentPage
{
    private readonly ILogService _logService;

    public ObservableCollection<string> LogLines { get; } = [];

    public DebugRuntimeConsolePage()
    {
        InitializeComponent();
        _logService = MauiProgram.Services.GetRequiredService<ILogService>();
        BindingContext = this;

        DebugModeSwitch.IsToggled = _logService.IsEnabled;
        DebugModeSwitch.Toggled += OnDebugModeToggled;
        StatusLabel.Text = BuildStatus();

        foreach (var entry in _logService.GetLogs())
        {
            LogLines.Add(FormatEntry(entry));
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _logService.LogAdded += OnLogAdded;
        StatusLabel.Text = BuildStatus();
        ScrollToBottom();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _logService.LogAdded -= OnLogAdded;
    }

    private void OnLogAdded(object? sender, Models.Runtime.RuntimeLogEntry entry)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LogLines.Add(FormatEntry(entry));
            StatusLabel.Text = BuildStatus();
            ScrollToBottom();
        });
    }

    private void OnDebugModeToggled(object? sender, ToggledEventArgs e)
    {
        _logService.IsEnabled = e.Value;
        StatusLabel.Text = BuildStatus();
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        _logService.Clear();
        LogLines.Clear();
        StatusLabel.Text = BuildStatus();
    }

    private string BuildStatus()
    {
        return $"Debug mode: {(_logService.IsEnabled ? "ON" : "OFF")} | Logs: {LogLines.Count}";
    }

    private static string FormatEntry(Models.Runtime.RuntimeLogEntry entry)
    {
        return $"[{entry.TimestampUtc:HH:mm:ss}] [{entry.Source}] {entry.Message}";
    }

    private void ScrollToBottom()
    {
        if (LogLines.Count == 0)
        {
            return;
        }

        LogsCollectionView.ScrollTo(LogLines.Count - 1, position: ScrollToPosition.End, animate: false);
    }
}

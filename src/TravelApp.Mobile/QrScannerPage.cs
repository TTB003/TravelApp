using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;
using TravelApp.Services;
using TravelApp.Services.Abstractions;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace TravelApp;

public sealed class QrScannerPage : ContentPage
{
    private readonly IQrCodeParserService _qrCodeParserService;
    private readonly IPoiApiClient _poiApiClient;
    private readonly CameraBarcodeReaderView _scannerView;
    private readonly Label _statusLabel;
    private bool _isHandlingScan;
    private readonly bool _showManualFallback;

    public QrScannerPage()
    {
        _qrCodeParserService = MauiProgram.Services.GetRequiredService<IQrCodeParserService>();
        _poiApiClient = MauiProgram.Services.GetRequiredService<IPoiApiClient>();

        _scannerView = new CameraBarcodeReaderView
        {
            IsDetecting = true,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormats.All,
                AutoRotate = true,
                Multiple = false
            }
        };
        _scannerView.BarcodesDetected += OnBarcodesDetected;

        _statusLabel = new Label
        {
            Text = "Đang chờ quét mã...",
            FontSize = 13,
            TextColor = Color.FromArgb("#E31667")
        };

        _showManualFallback = ShouldShowManualFallback();

        Content = BuildContent();
        BackgroundColor = Colors.Black;
        Shell.SetNavBarIsVisible(this, false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isHandlingScan = false;
        _scannerView.IsDetecting = true;
        _statusLabel.Text = "Đang chờ quét mã...";

        var permission = await Permissions.RequestAsync<Permissions.Camera>();
        if (permission != PermissionStatus.Granted)
        {
            _scannerView.IsDetecting = false;
            await DisplayAlert("Camera", "TravelApp cần quyền camera để quét mã QR.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    protected override void OnDisappearing()
    {
        _scannerView.IsDetecting = false;
        base.OnDisappearing();
    }

    private View BuildContent()
    {
        var topBar = new Grid
        {
            Padding = new Thickness(16, 52, 16, 12),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var closeButton = new Border
        {
            WidthRequest = 44,
            HeightRequest = 44,
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#E31667"),
            StrokeShape = new RoundRectangle { CornerRadius = 22 }
        };
        closeButton.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Shell.Current.GoToAsync("..")) });
        closeButton.Content = new Label
        {
            Text = "←",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        var titleBlock = new VerticalStackLayout
        {
            Spacing = 2,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        titleBlock.Children.Add(new Label
        {
            Text = "Quét mã QR",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center
        });
        titleBlock.Children.Add(new Label
        {
            Text = "Đưa mã QR vào khung để mở nhanh POI Detail",
            FontSize = 13,
            TextColor = Color.FromArgb("#D9DDE8"),
            HorizontalTextAlignment = TextAlignment.Center
        });

        var qrBadge = new Border
        {
            WidthRequest = 44,
            HeightRequest = 44,
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#FFFFFF22"),
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Content = new Label
            {
                Text = "QR",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };

        topBar.Add(closeButton);
        topBar.Add(titleBlock);
        topBar.Add(qrBadge);
        Grid.SetColumn(titleBlock, 1);
        Grid.SetColumn(qrBadge, 2);

        var scannerGrid = new Grid();
        scannerGrid.Children.Add(_scannerView);
        scannerGrid.Children.Add(new Grid
        {
            BackgroundColor = Color.FromArgb("#77000000"),
            Children =
            {
                new Border
                {
                    Stroke = Color.FromArgb("#E31667"),
                    StrokeThickness = 2,
                    StrokeShape = new RoundRectangle { CornerRadius = 24 },
                    WidthRequest = 260,
                    HeightRequest = 260,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    BackgroundColor = Colors.Transparent
                }
            }
        });

        var infoCard = new Border
        {
            Margin = new Thickness(16, 0, 16, 16),
            Padding = 14,
            StrokeThickness = 0,
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 20 }
        };
        var infoStack = new VerticalStackLayout { Spacing = 10 };
        infoStack.Children.Add(new Label
        {
            Text = "Lưu ý",
            FontAttributes = FontAttributes.Bold,
            FontSize = 16,
            TextColor = Color.FromArgb("#1B1F28")
        });
        infoStack.Children.Add(new Label
        {
            Text = "Mã QR hợp lệ có thể là số POI trực tiếp, URL có poiId, hoặc chuỗi chứa mã địa điểm.",
            FontSize = 13,
            TextColor = Color.FromArgb("#5D6472"),
            LineBreakMode = LineBreakMode.WordWrap
        });
        infoStack.Children.Add(_statusLabel);

        if (_showManualFallback)
        {
            infoStack.Children.Add(BuildFallbackSection());
        }

        infoCard.Content = infoStack;

        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };
        root.Children.Add(topBar);
        root.Children.Add(scannerGrid);
        root.Children.Add(infoCard);
        Grid.SetRow(scannerGrid, 1);
        Grid.SetRow(infoCard, 2);

        return root;
    }

    private View BuildFallbackSection()
    {
        var container = new VerticalStackLayout
        {
            Spacing = 10,
            Margin = new Thickness(0, 8, 0, 0)
        };

        container.Children.Add(new BoxView
        {
            HeightRequest = 1,
            Color = Color.FromArgb("#E8EBF2")
        });

        container.Children.Add(new Label
        {
            Text = "Fallback cho emulator / debug",
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1B1F28")
        });

        var pasteButton = new Button
        {
            Text = "Paste QR content",
            BackgroundColor = Color.FromArgb("#F7F8FB"),
            TextColor = Color.FromArgb("#1B1F28"),
            BorderColor = Color.FromArgb("#D6DCE8"),
            BorderWidth = 1,
            CornerRadius = 16,
            HeightRequest = 44
        };
        pasteButton.Clicked += async (_, _) => await PasteQrContentAsync();

        var inputButton = new Button
        {
            Text = "Nhập mã QR thủ công",
            BackgroundColor = Color.FromArgb("#E31667"),
            TextColor = Colors.White,
            CornerRadius = 16,
            HeightRequest = 44
        };
        inputButton.Clicked += async (_, _) => await PromptQrContentAsync();

        var buttonsGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        buttonsGrid.Children.Add(pasteButton);
        buttonsGrid.Children.Add(inputButton);
        Grid.SetColumn(inputButton, 1);

        container.Children.Add(buttonsGrid);

        container.Children.Add(new Label
        {
            Text = "Dùng khi emulator không có camera feed. Bạn có thể dán URL/ID rồi app sẽ mở POI Detail như quét QR thật.",
            FontSize = 12,
            TextColor = Color.FromArgb("#6E7380"),
            LineBreakMode = LineBreakMode.WordWrap
        });

        return container;
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_isHandlingScan)
        {
            return;
        }

        var scannedText = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(scannedText))
        {
            return;
        }

        _isHandlingScan = true;
        _scannerView.IsDetecting = false;

        MainThread.BeginInvokeOnMainThread(async () => await HandleScanAsync(scannedText));
    }

    private async Task HandleScanAsync(string scannedText)
    {
        try
        {
            _statusLabel.Text = "Đang xử lý mã QR...";

            var poiId = _qrCodeParserService.TryParsePoiId(scannedText);
            if (!poiId.HasValue)
            {
                _statusLabel.Text = "Mã QR không hợp lệ.";
                await DisplayAlert("QR code", "Không thể đọc mã POI từ QR này.", "OK");
                _isHandlingScan = false;
                _scannerView.IsDetecting = true;
                return;
            }

            var poi = await _poiApiClient.GetByIdAsync(poiId.Value, UserProfileService.PreferredLanguage);
            if (poi is null)
            {
                _statusLabel.Text = "Không tìm thấy địa điểm.";
                await DisplayAlert("QR code", $"Không tìm thấy POI có ID {poiId.Value}.", "OK");
                _isHandlingScan = false;
                _scannerView.IsDetecting = true;
                return;
            }

            _statusLabel.Text = $"Đã tìm thấy: {poi.Title}";
            
            // Ghi nhận lượt quét về server
            try {
                var config = MauiProgram.Services.GetRequiredService<AppConfig>();
                var client = new HttpClient();
                await client.PostAsync($"{config.ApiBaseUrl}web/api/pois/{poiId.Value}/qr-scan", null);
            } catch { /* Ignore error for analytics */ }

            await Shell.Current.GoToAsync("..");
            await Task.Delay(150);
            await Shell.Current.GoToAsync($"TourDetailPage?tourId={poiId.Value}");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Lỗi khi quét QR.";
            await DisplayAlert("QR code", $"Không thể mở POI từ QR: {ex.Message}", "OK");
            _isHandlingScan = false;
            _scannerView.IsDetecting = true;
        }
    }

    private async Task PromptQrContentAsync()
    {
        var qrContent = await DisplayPromptAsync(
            "Nhập mã QR",
            "Dán nội dung QR, URL, hoặc POI ID:",
            accept: "Xử lý",
            cancel: "Hủy",
            placeholder: "123 hoặc https://...poiId=123");

        if (string.IsNullOrWhiteSpace(qrContent))
        {
            return;
        }

        await HandleScanAsync(qrContent);
    }

    private async Task PasteQrContentAsync()
    {
        var qrContent = await Clipboard.Default.GetTextAsync();
        if (string.IsNullOrWhiteSpace(qrContent))
        {
            await DisplayAlert("Clipboard", "Không có nội dung để dán.", "OK");
            return;
        }

        await HandleScanAsync(qrContent);
    }

    private static bool ShouldShowManualFallback()
    {
        return Debugger.IsAttached || DeviceInfo.DeviceType == DeviceType.Virtual;
    }
}

namespace TravelApp;

public class AppConfig
{
    // Đã đồng bộ cổng 5001 cho toàn bộ hệ thống
    public string ApiBaseUrl { get; set; } = "http://192.168.100.164:5001/";

    // Host dùng chung cho các dịch vụ Web (nên để IP máy để Mobile truy cập được)
    public string AdminHost { get; set; } = "http://192.168.100.164";

    // Port cho trang Quản trị (Admin)
    public int AdminPort { get; set; } = 7020;

    // Port cho trang Web công khai (Người dùng quét QR sẽ vào đây)
    public int PublicWebPort { get; set; } = 7020;

    public string QuickChartQrBase { get; set; } = "https://quickchart.io/qr?size=400&text=";

    public string DefaultLanguage { get; set; } = "vi";

    public string[] SupportedLanguages { get; set; } = 
    { 
        "vi", "en", "fr", "ja" 
    };
}

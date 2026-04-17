namespace TravelApp;

public class AppConfig
{
    // Đã đồng bộ cổng 5001 cho toàn bộ hệ thống
    public string ApiBaseUrl { get; set; } = "http://192.168.100.164:5001/";

    // Sửa AdminHost và Port cho đúng với cái đang chạy trên máy bạn
    public string AdminHost { get; set; } = "http://192.168.100.164";

    // Kiểm tra lại: Nếu bạn chạy Admin Web mà thấy nó hiện localhost:5174 thì sửa thành 5174
    public int AdminPort { get; set; } = 5174;

    public string QuickChartQrBase { get; set; } = "https://quickchart.io/qr?size=400&text=";

    public string DefaultLanguage { get; set; } = "vi-VN";

    public string[] SupportedLanguages { get; set; } = 
    { 
        "vi-VN", "en-US", "fr-FR", "ja-JP", "ko-KR", "zh-CN", "de-DE", "es-ES" 
    };
}

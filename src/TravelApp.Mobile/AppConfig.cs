namespace TravelApp;

public class AppConfig
{
    // Sửa cổng từ 5001 thành 5293 cho khớp với launchSettings.json
    public string ApiBaseUrl { get; set; } = "http://192.168.100.164:5001/";

    // Sửa AdminHost và Port cho đúng với cái đang chạy trên máy bạn
    public string AdminHost { get; set; } = "http://192.168.100.164";

    // Kiểm tra lại: Nếu bạn chạy Admin Web mà thấy nó hiện localhost:5174 thì sửa thành 5174
    public int AdminPort { get; set; } = 5174;

    public string QuickChartQrBase { get; set; } = "https://quickchart.io/qr?size=400&text=";
}

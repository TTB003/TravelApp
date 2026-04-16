namespace TravelApp;

public class AppConfig
{
    public string ApiBaseUrl { get; set; } = "http://192.168.5.36:5001/";
    public string AdminHost { get; set; } = "http://192.168.5.36";
    public int AdminPort { get; set; } = 7020;
    public string QuickChartQrBase { get; set; } = "https://quickchart.io/qr?size=400&text=";
}

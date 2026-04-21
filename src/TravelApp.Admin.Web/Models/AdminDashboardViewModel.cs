namespace TravelApp.Admin.Web.Models;

public class AdminDashboardViewModel
{
    public int PoiCount { get; set; }
    public int UserCount { get; set; }
    public int PublishedTourCount { get; set; } // Sẽ dùng để chứa tổng lượt nghe Audio
    public int QrCount { get; set; }            // Sẽ dùng để chứa tổng lượt quét QR
    public string ApiBaseUrl { get; set; } = string.Empty;

    public List<DashboardPoiSummary> RecentPois { get; set; } = new();
}

public class DashboardStatsDto
{
    public int PoiCount { get; set; }
    public int UserCount { get; set; }
    public int PublishedTourCount { get; set; }
    public int QrCount { get; set; }
}

public class PoiStatDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int QrScans { get; set; }
    public int AudioPlays { get; set; }
}

public class DashboardPoiSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int AudioPlays { get; set; } // Thêm trường này
    public int QrScans { get; set; }    // Thêm trường này
}
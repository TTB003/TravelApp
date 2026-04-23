using System.Collections.Generic;

namespace TravelApp.Admin.Web.Models;

public class AdminDashboardViewModel
{
    public int PoiCount { get; set; }
    public int UserCount { get; set; }
    
    // Tổng lượt nghe Audio
    public int PublishedTourCount { get; set; }
    
    // Tổng lượt quét QR
    public int QrCount { get; set; }
    
    public string ApiBaseUrl { get; set; } = string.Empty;

    // Các thuộc tính mới để hiển thị User Online
    public int OnlineUserCount { get; set; }
    public List<OnlineUserDisplayDto> OnlineUsers { get; set; } = new();

    public List<DashboardPoiSummary> RecentPois { get; set; } = new();
}

public class DashboardPoiSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int AudioPlays { get; set; }
    public int QrScans { get; set; }
}
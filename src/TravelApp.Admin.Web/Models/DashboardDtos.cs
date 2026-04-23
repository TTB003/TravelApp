namespace TravelApp.Admin.Web.Models;

/// <summary>
/// DTO chứa thông tin thống kê tổng quát từ API cho Dashboard.
/// </summary>
public class DashboardStatsDto
{
    public int PoiCount { get; set; }
    public int UserCount { get; set; }
    public int PublishedTourCount { get; set; }
    public int QrCount { get; set; }
}

/// <summary>
/// DTO chứa thông tin thống kê của từng địa điểm (POI).
/// </summary>
public class PoiStatDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int QrScans { get; set; }
    public int AudioPlays { get; set; }
}
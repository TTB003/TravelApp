using TravelApp.Application.Dtos.Metrics;

namespace TravelApp.Admin.Web.ViewModels.Dashboard;

public class DashboardViewModel
{
    public long TotalPoiViews { get; set; }
    public long TotalPoiPlays { get; set; }
    public long TotalTourViews { get; set; }
    public long TotalTourPlays { get; set; }
    public long TotalQrScans { get; set; }

    public List<EventAdminDto> RecentEvents { get; set; } = new();
    public List<object> TopPois { get; set; } = new();
}

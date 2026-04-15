using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelApp.Admin.Web.Services;

namespace TravelApp.Admin.Web.Controllers;

[Authorize(Roles = "Owner,Admin,SuperAdmin")]
public class DashboardController : Controller
{
    private readonly ITravelAppApiClient _apiClient;

    public DashboardController(ITravelAppApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new ViewModels.Dashboard.DashboardViewModel();

        var overview = await _apiClient.GetMetricsOverviewAsync(cancellationToken);
        if (overview is not null)
        {
            model.TotalPoiViews = overview.TotalPoiViews;
            model.TotalPoiPlays = overview.TotalPoiPlays;
            model.TotalTourViews = overview.TotalTourViews;
            model.TotalTourPlays = overview.TotalTourPlays;
            model.TotalQrScans = overview.TotalQrScans;
        }

        model.RecentEvents = (await _apiClient.GetRecentEventsAsync(50, cancellationToken)).ToList();
        model.TopPois = (await _apiClient.GetTopPoisAsync(10, cancellationToken)).Cast<object>().ToList();

        return View(model);
    }
}

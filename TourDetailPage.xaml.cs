using TravelApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace TravelApp;

public partial class TourDetailPage : ContentPage, IQueryAttributable
{
    private readonly TourDetailViewModel _viewModel;

    public TourDetailPage()
    {
        InitializeComponent();
        _viewModel = MauiProgram.Services.GetRequiredService<TourDetailViewModel>();
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("tourId", out var tourId))
        {
            _viewModel.Load(tourId?.ToString());
        }
    }
}

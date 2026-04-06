using TravelApp.ViewModels;

namespace TravelApp;

public partial class SearchPage : ContentPage
{
    public SearchPage()
    {
        InitializeComponent();
        var vm = MauiProgram.Services.GetService<SearchViewModel>()!;
        BindingContext = vm;
        _ = vm.InitializeAsync();
    }
}

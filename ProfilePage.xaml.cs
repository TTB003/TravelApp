using TravelApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace TravelApp;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<ProfileViewModel>();
    }
}

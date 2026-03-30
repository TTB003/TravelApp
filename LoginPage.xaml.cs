using TravelApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace TravelApp;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<LoginViewModel>();
    }
}

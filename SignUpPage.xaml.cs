using TravelApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace TravelApp;

public partial class SignUpPage : ContentPage
{
    public SignUpPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<SignUpViewModel>();
    }
}

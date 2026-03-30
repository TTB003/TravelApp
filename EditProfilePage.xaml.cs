using TravelApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace TravelApp;

public partial class EditProfilePage : ContentPage
{
    public EditProfilePage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<EditProfileViewModel>();
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace TravelApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(MauiProgram.Services.GetRequiredService<AppShell>());
        }
    }
}
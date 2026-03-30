using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using TravelApp.Services.Abstractions;
using TravelApp.Services.Api;
using TravelApp.Services.Runtime;
using TravelApp.ViewModels;

namespace TravelApp
{
    public static class MauiProgram
    {
        public static IServiceProvider Services { get; private set; } = default!;

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .AddAudio()
                .UseMauiMaps()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton(new ApiClientOptions
            {
                BaseUrl = "https://api.your-domain.com/"
            });

            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<ITokenStore, InMemoryTokenStore>();
            builder.Services.AddTransient<IAuthApiClient, AuthApiClient>();
            builder.Services.AddTransient<IProfileApiClient, ProfileApiClient>();
            builder.Services.AddTransient<IPoiApiClient, PoiApiClient>();

            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<ILogService, RuntimeLogService>();
            builder.Services.AddSingleton<ILocationProvider, MauiLocationProvider>();
            builder.Services.AddSingleton<ILocationTrackerService, LocationTrackerService>();
            builder.Services.AddSingleton<IGeofenceService, GeofenceService>();
            builder.Services.AddSingleton<IAudioPlayerService, AudioPlayerService>();
            builder.Services.AddSingleton<IAutoAudioTriggerService, AutoAudioTriggerService>();
            builder.Services.AddSingleton<ITravelRuntimePipeline, TravelRuntimePipeline>();
            builder.Services.AddSingleton<ITravelBootstrapService, TravelBootstrapService>();

            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<SignUpViewModel>();
            builder.Services.AddTransient<ExploreViewModel>();
            builder.Services.AddTransient<TourDetailViewModel>();
            builder.Services.AddTransient<ProfileViewModel>();
            builder.Services.AddTransient<EditProfileViewModel>();

            builder.Services.AddSingleton<AppShell>();

            var app = builder.Build();
            Services = app.Services;
            return app;
        }
    }
}

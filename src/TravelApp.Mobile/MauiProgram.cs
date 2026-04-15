using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using TravelApp.Handlers;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;
using TravelApp.Services.Api;
using TravelApp.Services.Runtime;
using TravelApp.ViewModels;
using ZXing.Net.Maui.Controls;

namespace TravelApp
{
    public static class MauiProgram
    {
        public static IServiceProvider Services { get; private set; } = default!;

        private static bool CanUseMapsOnWindows()
        {
            var bingMapsKey = Environment.GetEnvironmentVariable("BING_MAPS_KEY");
            return !string.IsNullOrWhiteSpace(bingMapsKey);
        }

        private static string ResolveApiBaseUrl()
        {
#if DEBUG
            // Use the developer machine IP so physical devices and emulators can reach the API.
            // The server will be configured to listen on http://0.0.0.0:5001.
            return "http://192.168.5.36:5001/";
#else
            return "https://api.your-domain.com/";
#endif
        }

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeReader()
                .AddAudio()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if WINDOWS
            if (CanUseMapsOnWindows())
            {
                builder.UseMauiMaps();
            }
#else
            builder.UseMauiMaps();
#endif

            MapPinAppearanceHandler.Register();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton(new ApiClientOptions
            {
                BaseUrl = ResolveApiBaseUrl()
            });
            builder.Services.AddSingleton(new CachePolicyOptions
            {
                Mode = CacheMode.OfflineFirst
            });

            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<ITokenStore, InMemoryTokenStore>();
            builder.Services.AddSingleton<ILocalDatabaseService, LocalDatabaseService>();
            builder.Services.AddSingleton<TourRouteCacheService>();
            builder.Services.AddSingleton<ITourRouteCacheService>(sp => sp.GetRequiredService<TourRouteCacheService>());
            builder.Services.AddTransient<IAuthApiClient, AuthApiClient>();
            builder.Services.AddTransient<IProfileApiClient, ProfileApiClient>();
            builder.Services.AddTransient<IPoiApiClient, PoiApiClient>();
            builder.Services.AddTransient<ITourApiClient, TourApiClient>();
            builder.Services.AddTransient<ITourRouteCatalogService, TourRouteCatalogService>();
            builder.Services.AddSingleton<ITourRouteGeometryService, AzureMapsRouteGeometryService>();
            builder.Services.AddTransient<IPoiApiService, PoiApiService>();
            builder.Services.AddSingleton<IQrCodeParserService, QrCodeParserService>();

            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<ILogService, RuntimeLogService>();
            builder.Services.AddSingleton<ILocationProvider, MauiLocationProvider>();
            builder.Services.AddSingleton<ILocationPollingService, LocationPollingService>();
            builder.Services.AddSingleton<ILocationTrackerService, LocationTrackerService>();
            builder.Services.AddSingleton<IGeofenceService, GeofenceService>();
            builder.Services.AddSingleton<IPoiGeofenceService, PoiGeofenceService>();
            builder.Services.AddSingleton<IAudioPlayerService, AudioPlayerService>();
            builder.Services.AddSingleton<IAudioLibraryService, AudioLibraryService>();
            builder.Services.AddSingleton<IBookmarkHistoryService, BookmarkHistoryService>();
            builder.Services.AddSingleton<ITourMapRouteService, TourMapRouteService>();
            builder.Services.AddSingleton<IAutoAudioTriggerService, AutoAudioTriggerService>();
            builder.Services.AddSingleton<ITourRoutePlaybackService, TourRoutePlaybackService>();
            builder.Services.AddSingleton<IAudioService, AudioService>();
            builder.Services.AddSingleton<ITravelRuntimePipeline, TravelRuntimePipeline>();
            builder.Services.AddSingleton<ITravelBootstrapService, TravelBootstrapService>();

            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<ExploreViewModel>();
            builder.Services.AddTransient<SearchViewModel>();
            builder.Services.AddTransient<TourDetailViewModel>();
            builder.Services.AddTransient<ProfileViewModel>();
            builder.Services.AddTransient<EditProfileViewModel>();
            builder.Services.AddTransient<PoiListViewModel>();
            builder.Services.AddTransient<NowPlayingViewModel>();
            builder.Services.AddTransient<MyAudioLibraryViewModel>();
            builder.Services.AddTransient<BookmarksHistoryViewModel>();
            builder.Services.AddTransient<TourMapRouteViewModel>();
            builder.Services.AddTransient<MapViewModel>();
            builder.Services.AddTransient<QrScannerPage>();

            builder.Services.AddSingleton<AppShell>();

            var app = builder.Build();
            Services = app.Services;
            return app;
        }
    }
}

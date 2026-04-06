using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using System;
using System.Net.Http;
using Plugin.Maui.Audio;
using TravelApp.Models.Runtime;
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

            // Configure API base URL per platform with optional environment override.
            string? envBaseUrl = Environment.GetEnvironmentVariable("TRAVELAPP_API_BASEURL");
            string baseUrl;
            if (!string.IsNullOrWhiteSpace(envBaseUrl))
            {
                baseUrl = envBaseUrl!;
            }
            else if (OperatingSystem.IsAndroid())
            {
                // Android emulator -> host machine
                baseUrl = "https://10.0.2.2:7145/";
            }
            else if (OperatingSystem.IsIOS() || OperatingSystem.IsMacOS())
            {
                // iOS simulator / macOS
                baseUrl = "https://localhost:7145/";
            }
            else
            {
                // Windows or other
                baseUrl = "https://localhost:7145/";
            }

            builder.Services.AddSingleton(new ApiClientOptions
            {
                BaseUrl = baseUrl
            });
            builder.Services.AddSingleton(new CachePolicyOptions
            {
                Mode = CacheMode.OfflineFirst
            });

            // --- THAY THẾ ĐOẠN ĐĂNG KÝ HTTPCLIENT BẰNG ĐOẠN NÀY ---

            // Cấu hình Handler bỏ qua SSL (chỉ dùng cho Debug Android)
            var handler = new HttpClientHandler();
#if DEBUG
            if (OperatingSystem.IsAndroid())
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }
#endif

            // Đăng ký HttpClient theo cách chuẩn nhất của .NET
            // Cách này vừa giúp AudioService không crash, vừa giúp ApiClient có bùa SSL
            builder.Services.AddHttpClient("", client => 
            {
                client.BaseAddress = new Uri(baseUrl);
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);

            // Đăng ký một bản Singleton HttpClient để hỗ trợ các class đời cũ gọi trực tiếp (nếu có)
            builder.Services.AddSingleton(sp => 
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return factory.CreateClient("");
            });

            // --- KẾT THÚC ĐOẠN THAY THẾ ---
            builder.Services.AddSingleton<ITokenStore, InMemoryTokenStore>();
            builder.Services.AddSingleton<ILocalDatabaseService, LocalDatabaseService>();
            builder.Services.AddTransient<IAuthApiClient, AuthApiClient>();
            builder.Services.AddTransient<IProfileApiClient, ProfileApiClient>();
            builder.Services.AddTransient<IPoiApiClient, PoiApiClient>();
            builder.Services.AddTransient<IPoiApiService, PoiApiService>();

            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<ILogService, RuntimeLogService>();
            builder.Services.AddSingleton<ILocationProvider, MauiLocationProvider>();
            builder.Services.AddSingleton<ILocationPollingService, LocationPollingService>();
            builder.Services.AddSingleton<ILocationTrackerService, LocationTrackerService>();
            builder.Services.AddSingleton<IGeofenceService, GeofenceService>();
            builder.Services.AddSingleton<IPoiGeofenceService, PoiGeofenceService>();
            builder.Services.AddSingleton<IAudioPlayerService, AudioPlayerService>();
            builder.Services.AddSingleton<IAutoAudioTriggerService, AutoAudioTriggerService>();
            builder.Services.AddSingleton<IAudioService, AudioService>();
            builder.Services.AddSingleton<ITextToSpeechService, GoogleTtsService>();
            builder.Services.AddSingleton<ITravelRuntimePipeline, TravelRuntimePipeline>();
            builder.Services.AddSingleton<ITravelBootstrapService, TravelBootstrapService>();

            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<SignUpViewModel>();
            builder.Services.AddTransient<ExploreViewModel>();
            builder.Services.AddTransient<SearchViewModel>();
            builder.Services.AddTransient<TourDetailViewModel>();
            builder.Services.AddTransient<ProfileViewModel>();
            builder.Services.AddTransient<EditProfileViewModel>();
            builder.Services.AddTransient<PoiListViewModel>();

            builder.Services.AddSingleton<AppShell>();

            var app = builder.Build();
            Services = app.Services;
            return app;
        }
    }
}

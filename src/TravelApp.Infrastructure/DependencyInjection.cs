using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelApp.Application.Abstractions.Persistence;
using TravelApp.Application.Abstractions.Pois;
using TravelApp.Infrastructure.Persistence;
using TravelApp.Infrastructure.Services.Pois;

namespace TravelApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<TravelAppDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        // Provide IHttpClientFactory for external API calls (e.g., Google Translate)
        services.AddHttpClient();

        // Register translation service implementation
        services.AddScoped<TravelApp.Application.Abstractions.ITranslationService, TravelApp.Infrastructure.Services.Translation.GoogleTranslationService>();

        services.AddScoped<ITravelAppDbContext>(provider => provider.GetRequiredService<TravelAppDbContext>());
        services.AddScoped<IPoiQueryService, PoiQueryService>();
        // register translation service for server-side translation
        services.AddScoped<TravelApp.Application.Abstractions.ITranslationService, TravelApp.Infrastructure.Services.Translation.GoogleTranslationService>();

        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Services.Discord;
using NoMercyBot.Services.Obs;
using NoMercyBot.Services.Other;
using NoMercyBot.Services.Seeds;
using NoMercyBot.Services.Spotify;
using NoMercyBot.Services.Twitch;

namespace NoMercyBot.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBotServices(this IServiceCollection services)
    {
        services.AddSingleton<SeedService>();
        
        services.AddTwitchServices();
        services.AddSpotifyServices();
        services.AddDiscordServices();
        services.AddObsServices();
        services.AddOtherServices();
        
        services.AddTokenRefreshService();
        
        return services;
    }

    private static IServiceCollection AddOtherServices(this IServiceCollection services)
    {
        services.AddScoped<PronounService>();
        return services;
    }

    private static IServiceCollection AddTokenRefreshService(this IServiceCollection services)
    {
        services.AddHostedService<TokenRefreshService>();
        return services;
    }
}
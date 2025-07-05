using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Services.Discord;
using NoMercyBot.Services.Obs;
using NoMercyBot.Services.Spotify;
using NoMercyBot.Services.Twitch;

namespace NoMercyBot.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBotServices(this IServiceCollection services)
    {
        services.AddTwitchServices();
        services.AddSpotifyServices();
        services.AddDiscordServices();
        services.AddObsServices();
        
        services.AddTokenRefreshService();
        services.AddCustomLogging();
        
        return services;
    }

    private static IServiceCollection AddTokenRefreshService(this IServiceCollection services)
    {
        services.AddHostedService<TokenRefreshService>();
        return services;
    }
    
    public static IServiceCollection AddCustomLogging(this IServiceCollection services)
    {
        // Replace the standard logger with your custom implementation
        services.AddSingleton(typeof(ILogger<>), typeof(CustomLogger<>));
        return services;
    }
}
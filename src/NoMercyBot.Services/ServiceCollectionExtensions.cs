using Microsoft.Extensions.DependencyInjection;
using NoMercyBot.Services.Discord;
using NoMercyBot.Services.Obs;
using NoMercyBot.Services.Other;
using NoMercyBot.Services.Seeds;
using NoMercyBot.Services.Spotify;
using NoMercyBot.Services.Twitch;
using NoMercyBot.Services.Emotes;
using Microsoft.Extensions.Hosting;

namespace NoMercyBot.Services;

public static class ServiceCollectionExtensions
{
    public static void AddBotServices(this IServiceCollection services)
    {
        services.AddSingleton<SeedService>();
        
        services.AddTwitchServices();
        services.AddSpotifyServices();
        services.AddDiscordServices();
        services.AddObsServices();
        services.AddOtherServices();
        services.AddEmoteServices();
        
        services.AddTokenRefreshService();
    }

    private static void AddOtherServices(this IServiceCollection services)
    {
        services.AddScoped<PronounService>();
        services.AddScoped<BotAuthService>();
    }
    
    private static void AddEmoteServices(this IServiceCollection services)
    {
        services.AddSingletonHostedService<BttvService>();
        services.AddSingletonHostedService<FrankerFacezService>();
        services.AddSingletonHostedService<SevenTvService>();
    }

    private static void AddTokenRefreshService(this IServiceCollection services)
    {
        services.AddHostedService<TokenRefreshService>();
    }

    // Extension method to add a service as both a singleton and a hosted service
    private static IServiceCollection AddSingletonHostedService<TService>(this IServiceCollection services)
        where TService : class, IHostedService
    {
        services.AddSingleton<TService>();
        services.AddHostedService(provider => provider.GetRequiredService<TService>());
        return services;
    }
}
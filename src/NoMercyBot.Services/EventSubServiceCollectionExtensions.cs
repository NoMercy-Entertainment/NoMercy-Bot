using Microsoft.Extensions.DependencyInjection;
using NoMercyBot.Services.Discord;
using NoMercyBot.Services.Obs;
using NoMercyBot.Services.Twitch;

namespace NoMercyBot.Services;

public static class EventSubServiceCollectionExtensions
{
    public static IServiceCollection AddEventSubServices(this IServiceCollection services)
    {
        // Register the EventSub services
        services.AddScoped<TwitchEventSubService>();
        services.AddScoped<DiscordEventSubService>();
        services.AddScoped<ObsEventSubService>();
        
        return services;
    }
}

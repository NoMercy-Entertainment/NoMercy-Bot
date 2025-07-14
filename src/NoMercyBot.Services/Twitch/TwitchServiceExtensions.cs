using Microsoft.Extensions.DependencyInjection;

namespace NoMercyBot.Services.Twitch;

public static class TwitchServiceExtensions
{
    public static IServiceCollection AddTwitchServices(this IServiceCollection services)
    {
        services.AddSingleton<TwitchAuthService>();
        services.AddSingleton<BotAuthService>();
        services.AddSingleton<TwitchApiService>();
        services.AddSingleton<IAuthService>(sp => sp.GetRequiredService<TwitchAuthService>());

        return services;
    }
}
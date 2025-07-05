using NoMercyBot.Database.Models;
using NoMercyBot.Globals.Information;

namespace NoMercyBot.Services.Discord;

public class DiscordConfig: IConfig
{
    internal static Service? _service;

    protected internal static Service Service()
    {
        return _service ??= new();
    }
    
    public bool IsEnabled => Service().Enabled;
    
    public static string ApiUrl { get; } = "https://discord.com/api/v9";
    public static string AuthUrl { get; } = $"https://discord.com/api/v9/oauth2/token";
    
    public static string RedirectUri => $"http://localhost:{Config.InternalClientPort}/oauth/discord/callback";
    public static string EventSubCallbackUri => $"http://localhost:{Config.InternalServerPort}/eventsub/discord";
    
    public static string[] AvailableScopes { get; } =
    [
        
    ];
}
using NoMercyBot.Database.Models;
using NoMercyBot.Globals.Information;

namespace NoMercyBot.Services.Twitch;

public class TwitchConfig: IConfig
{
    internal static Service? _service;

    public static Service Service()
    {
        return _service ??= new();
    }
    
    public bool IsEnabled => Service().Enabled;
    
    public static string ApiUrl { get; } = "https://api.twitch.tv/helix";
    public static string AuthUrl { get; } = "https://id.twitch.tv/oauth2";
    
    public static string RedirectUri => $"http://localhost:{Config.InternalClientPort}/oauth/twitch/callback";
    public  string EventSubCallbackUri => $"http://localhost:{Config.InternalServerPort}/eventsub/twitch";

    public string[] AvailableScopes { get; } =
    [
        "channel:read:subscriptions",
        "chat:edit",
        "chat:read",
        "moderation:read",
        "moderator:manage:announcements",
        "moderator:manage:banned_users",
        "moderator:manage:blocked_terms",
        "moderator:manage:chat_messages",
        "moderator:manage:chat_settings",
        "moderator:manage:shoutouts",
        "moderator:manage:warnings",
        "moderator:read:chat_messages",
        "moderator:read:chat_settings",
        "moderator:read:chatters",
        "moderator:read:followers",
        "moderator:read:shoutouts",
        "moderator:read:warnings",
        "user:read:moderated_channels",
        "user:read:subscriptions",
        "user:write:chat"
    ];
}
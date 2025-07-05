using NoMercyBot.Database.Models;

namespace NoMercyBot.Services;

public interface IConfig
{
    static Service Service { get; }
    
    static bool IsEnabled { get; }
    static string ApiUrl { get; }
    static string AuthUrl { get; }
    static string RedirectUri { get; }
    static string EventSubCallbackUri { get; }
    static string[] AvailableScopes { get; }
}

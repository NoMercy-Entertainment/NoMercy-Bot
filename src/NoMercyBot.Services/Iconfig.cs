using NoMercyBot.Database.Models;

namespace NoMercyBot.Services;

public interface IConfig
{
    static Service _service { get; }
    static Service Service() => _service;
    
    static bool IsEnabled => true;
    static string ApiUrl { get; }
    static string AuthUrl { get; }
    static string RedirectUri { get; }
    static string EventSubCallbackUri { get; }
    static string[] AvailableScopes { get; }
}

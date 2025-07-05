using NoMercyBot.Database.Models;
using NoMercyBot.Globals.Information;

namespace NoMercyBot.Services.Spotify;

public class SpotifyConfig: IConfig
{
    internal static Service? _service;

    protected internal static Service Service()
    {
        return _service ??= new();
    }
    
    public bool IsEnabled => Service().Enabled;
    
    public static string ApiUrl { get; } = "https://api.spotify.com/v1";
    public static string AuthUrl { get; } = "https://accounts.spotify.com/api";
    
    public static string RedirectUri => $"http://localhost:{Config.InternalClientPort}/oauth/spotify/callback";
    public string EventSubCallbackUri => $"http://localhost:{Config.InternalServerPort}/eventsub/spotify";
    
    public string[] AvailableScopes { get; } =
    [
        "playlist-read-private",
        "playlist-read-collaborative",
        "ugc-image-upload",
        "user-follow-read",
        "playlist-modify-private",
        "user-read-email",
        "user-read-private",
        "app-remote-control",
        "streaming",
        "user-modify-playback-state",
        "user-follow-modify",
        "user-library-read",
        "user-library-modify",
        "playlist-modify-public",
        "user-read-playback-state",
        "user-read-currently-playing",
        "user-read-recently-played",
        "user-read-playback-position",
        "user-top-read"
    ];
}
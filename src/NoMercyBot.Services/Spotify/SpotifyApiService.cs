using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database.Models;

namespace NoMercyBot.Services.Spotify;

public class SpotifyApiService
{
    private readonly IConfiguration _conf;
    private readonly ILogger<SpotifyApiService> _logger;

    public Service Service => SpotifyConfig.Service();
    
    public string ClientId => Service.ClientId ?? throw new InvalidOperationException("Spotify ClientId is not set.");
    
    public SpotifyApiService(IConfiguration conf, ILogger<SpotifyApiService> logger)
    {
        _conf = conf;
        _logger = logger;
    }
}
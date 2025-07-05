using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database.Models;

namespace NoMercyBot.Services.Discord;

public class DiscordApiService
{
    private readonly IConfiguration _conf;
    private readonly ILogger<DiscordApiService> _logger;
    
    public Service Service => DiscordConfig.Service();
    
    public string ClientId => Service.ClientId ?? throw new InvalidOperationException("Discord ClientId is not set.");

    public DiscordApiService(IConfiguration conf, ILogger<DiscordApiService> logger)
    {
        _conf = conf;
        _logger = logger;
    }
}
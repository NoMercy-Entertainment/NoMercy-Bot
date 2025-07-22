using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;

namespace NoMercyBot.Services.Spotify;

public class SpotifyApiService
{
    private readonly IServiceScope _scope;
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _conf;
    private readonly ILogger<SpotifyApiService> _logger;

    public Service Service => SpotifyConfig.Service();
    
    public string ClientId => Service.ClientId ?? throw new InvalidOperationException("Spotify ClientId is not set.");
    
    public SpotifyApiService(IServiceScopeFactory serviceScopeFactory, IConfiguration conf, ILogger<SpotifyApiService> logger)
    {
        _scope = serviceScopeFactory.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _conf = conf;
        _logger = logger;
    }
}
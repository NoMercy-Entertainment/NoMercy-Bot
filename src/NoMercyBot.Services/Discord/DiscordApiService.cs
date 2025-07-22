using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;

namespace NoMercyBot.Services.Discord;

public class DiscordApiService
{
    private readonly IServiceScope _scope;
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _conf;
    private readonly ILogger<DiscordApiService> _logger;
    
    public Service Service => DiscordConfig.Service();
    
    public string ClientId => Service.ClientId ?? throw new InvalidOperationException("Discord ClientId is not set.");

    public DiscordApiService(IServiceScopeFactory serviceScopeFactory, IConfiguration conf, ILogger<DiscordApiService> logger)
    {
        _scope = serviceScopeFactory.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _conf = conf;
        _logger = logger;
    }
}
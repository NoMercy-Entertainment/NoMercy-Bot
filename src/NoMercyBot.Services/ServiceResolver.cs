using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Services.Discord;
using NoMercyBot.Services.Obs;
using NoMercyBot.Services.Spotify;
using NoMercyBot.Services.Twitch;

namespace NoMercyBot.Services;

public class ServiceResolver
{
    private readonly ILogger<ServiceResolver> _logger;
    private readonly IServiceScope _scope;
    private readonly AppDbContext _dbContext;

    public ServiceResolver(IServiceScopeFactory serviceScopeFactory, ILogger<ServiceResolver> logger)
    {
        _scope = serviceScopeFactory.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _logger = logger;
    }

    private async Task InitializeTwitch()
    {
        // Force reload from database, ignoring any cached/tracked entities
        Service? service = await _dbContext.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name == "Twitch");
            
        if (service != null)
        {
            TwitchConfig._service = service;
            _logger.LogInformation("Twitch service initialized. Enabled: {Enabled}, UserId: {UserId}, UserName: {UserName}", 
                service.Enabled, service.UserId, service.UserName);
        }
        else
        {
            _logger.LogWarning("Twitch service not found in database");
        }
    }

    private async Task InitializeSpotify()
    {
        Service? service = await _dbContext.Services.FirstOrDefaultAsync(s => s.Name == "Spotify");
        if (service != null)
        {
            SpotifyConfig._service = service;
            _logger.LogInformation("Spotify service initialized. Enabled: {Enabled}", service.Enabled);
        }
        else
        {
            _logger.LogWarning("Spotify service not found in database");
        }
    }

    private async Task InitializeDiscord()
    {
        Service? service = await _dbContext.Services.FirstOrDefaultAsync(s => s.Name == "Discord");
        if (service != null)
        {
            DiscordConfig._service = service;
            _logger.LogInformation("Discord service initialized. Enabled: {Enabled}", service.Enabled);
        }
        else
        {
            _logger.LogWarning("Discord service not found in database");
        }
    }

    private async Task InitializeObs()
    {
        Service? service = await _dbContext.Services.FirstOrDefaultAsync(s => s.Name == "OBS");
        if (service != null)
        {
            ObsConfig._service = service;
            _logger.LogInformation("OBS service initialized. Enabled: {Enabled}", service.Enabled);
        }
        else
        {
            _logger.LogWarning("OBS service not found in database");
        }
    }

    private async Task InitializeBotProvider()
    {
        BotAccount? botAccount = await _dbContext.BotAccounts.FirstOrDefaultAsync();
        if (botAccount != null)
        {
            // Validate bot's OAuth credentials
            bool isValid = ValidateBotOAuth(botAccount);
            if (isValid)
            {
                _logger.LogInformation("Bot provider initialized with username: {Username}", botAccount.Username);
            }
            else
            {
                _logger.LogWarning("Bot provider's OAuth credentials are invalid.");
            }
        }
        else
        {
            _logger.LogWarning("No bot provider configured.");
        }
    }

    private bool ValidateBotOAuth(BotAccount botAccount)
    {
        return !string.IsNullOrEmpty(botAccount.AccessToken) && botAccount.TokenExpiry.HasValue && botAccount.TokenExpiry.Value > DateTime.UtcNow;
    }

    public async Task InitializeAllServices()
    {
        _dbContext.ChangeTracker.Clear();
        
        await InitializeTwitch();
        await InitializeBotProvider();
        await InitializeSpotify();
        await InitializeDiscord();
        await InitializeObs();
    }
}
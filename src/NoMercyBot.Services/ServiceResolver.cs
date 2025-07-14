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
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ServiceResolver> _logger;

    public ServiceResolver(IServiceScopeFactory serviceScopeFactory, ILogger<ServiceResolver> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    private async Task InitializeTwitch(AppDbContext dbContext)
    {
        Service? service = await dbContext.Services.FirstOrDefaultAsync(s => s.Name == "Twitch");
        if (service != null)
        {
            TwitchConfig._service = service;
            _logger.LogInformation("Twitch service initialized. Enabled: {Enabled}", service.Enabled);
        }
    }

    private async Task InitializeSpotify(AppDbContext dbContext)
    {
        Service? service = await dbContext.Services.FirstOrDefaultAsync(s => s.Name == "Spotify");
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

    private async Task InitializeDiscord(AppDbContext dbContext)
    {
        Service? service = await dbContext.Services.FirstOrDefaultAsync(s => s.Name == "Discord");
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

    private async Task InitializeObs(AppDbContext dbContext)
    {
        Service? service = await dbContext.Services.FirstOrDefaultAsync(s => s.Name == "OBS");
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

    private async Task InitializeBotProvider(AppDbContext dbContext)
    {
        BotAccount? botAccount = await dbContext.BotAccounts.FirstOrDefaultAsync();
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
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await InitializeTwitch(dbContext);
        await InitializeBotProvider(dbContext);
        await InitializeSpotify(dbContext);
        await InitializeDiscord(dbContext);
        await InitializeObs(dbContext);
    }
}
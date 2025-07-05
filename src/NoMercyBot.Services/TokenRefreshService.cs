using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Services.Discord;
using NoMercyBot.Services.Obs;
using NoMercyBot.Services.Spotify;
using NoMercyBot.Services.Twitch;
using NoMercyBot.Services.Twitch.Dto;

namespace NoMercyBot.Services;

public class TokenRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TokenRefreshService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _refreshThreshold = TimeSpan.FromMinutes(5);

    public TokenRefreshService(IServiceScopeFactory scopeFactory, ILogger<TokenRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token refresh service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshTokensIfNeeded(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while refreshing tokens");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task RefreshTokensIfNeeded(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        List<Service> services = await dbContext.Services.ToListAsync(cancellationToken);
        
        foreach (Service service in services)
        {
            if (string.IsNullOrEmpty(service.RefreshToken))
                continue;
                
            if (service.TokenExpiry == null)
                continue;
                
            DateTime expiryTime = service.TokenExpiry.Value;
            DateTime refreshTime = expiryTime.AddMinutes(-_refreshThreshold.TotalMinutes);
            
            if (DateTime.UtcNow >= refreshTime)
            {
                await RefreshServiceToken(service, scope, cancellationToken);
            }
        }
    }
    
    private async Task RefreshServiceToken(Service service, IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Refreshing token for service {ServiceName}", service.Name);
            
            IAuthService? authService = GetAuthServiceForProvider(service.Name, scope);
            
            if (authService == null)
            {
                _logger.LogWarning("No auth service found for {ServiceName}", service.Name);
                return;
            }

            (User, TokenResponse) tokenResponse = await authService.RefreshToken(service.RefreshToken);
            await authService.StoreTokens(tokenResponse.Item2);
            
            _logger.LogInformation("Successfully refreshed token for {ServiceName}", service.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token for service {ServiceName}", service.Name);
        }
    }
    
    private IAuthService? GetAuthServiceForProvider(string provider, IServiceScope scope)
    {
        return provider.ToLower() switch
        {
            "twitch" => scope.ServiceProvider.GetService<TwitchAuthService>(),
            "spotify" => scope.ServiceProvider.GetService<SpotifyAuthService>(),
            "discord" => scope.ServiceProvider.GetService<DiscordAuthService>(),
            "obs" => scope.ServiceProvider.GetService<ObsAuthService>(),
            _ => null
        };
    }
}
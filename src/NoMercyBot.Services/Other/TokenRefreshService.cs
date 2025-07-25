﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Services.Discord;
using NoMercyBot.Services.Interfaces;
using NoMercyBot.Services.Obs;
using NoMercyBot.Services.Spotify;
using NoMercyBot.Services.Twitch;
using NoMercyBot.Services.Twitch.Dto;

namespace NoMercyBot.Services.Other;

public class TokenRefreshService : BackgroundService
{
    private readonly ILogger<TokenRefreshService> _logger;
    private readonly IServiceScope _scope;
    private readonly AppDbContext _dbContext;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _refreshThreshold = TimeSpan.FromMinutes(5);

    public TokenRefreshService(IServiceScopeFactory serviceScopeFactory, ILogger<TokenRefreshService> logger)
    {
        _scope = serviceScopeFactory.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
        List<Service> services = await _dbContext.Services.ToListAsync(cancellationToken);
        
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
                await RefreshServiceToken(service, _scope, cancellationToken);
            }
        }
    }
    
    private async Task RefreshServiceToken(Service service, IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            IAuthService? authService = GetAuthServiceForProvider(service.Name, scope);
            
            if (authService == null) return;
            
            _logger.LogDebug("Refreshing token for service {ServiceName}", service.Name);
            
            (User user, TokenResponse response) = await authService.RefreshToken(service.RefreshToken!);
            
            authService.Service.AccessToken = response.AccessToken;
            authService.Service.RefreshToken = response.RefreshToken;
            authService.Service.TokenExpiry = DateTime.UtcNow.AddSeconds(response.ExpiresIn);
            authService.Service.UserId = string.IsNullOrWhiteSpace(user.Id)
                ? authService.Service.UserId
                : user.Id;
            authService.Service.UserName = string.IsNullOrWhiteSpace(user.Username)
                ? authService.Service.UserName
                : user.Username;

            await _dbContext.SaveChangesAsync(cancellationToken);
            
            await _dbContext.Services.Upsert(authService.Service)
                .On(u => u.Name)
                .WhenMatched((oldService, newService) => new()
                {
                    AccessToken = newService.AccessToken,
                    RefreshToken = newService.RefreshToken,
                    TokenExpiry = newService.TokenExpiry,
                    UserId = newService.UserId,
                    UserName = newService.UserName,
                    UpdatedAt = DateTime.UtcNow,
                })
                .RunAsync(cancellationToken);
            
            _logger.LogDebug("Successfully refreshed token for {ServiceName}", service.Name);
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
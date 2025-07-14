using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Services.Twitch;
using NoMercyBot.Services.Twitch.Dto;

namespace NoMercyBot.Services;

public class BotAuthService : IAuthService
{
    private readonly IServiceScope _scope;
    private readonly ILogger<BotAuthService> _logger;
    private readonly AppDbContext _db;
    private readonly TwitchAuthService _twitchAuthService;

    public BotAuthService(
        IServiceScopeFactory serviceScopeFactory, 
        ILogger<BotAuthService> logger, 
        TwitchAuthService twitchAuthService)
    {
        _scope = serviceScopeFactory.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _logger = logger;
        _twitchAuthService = twitchAuthService;
    }

    // For bot accounts, we use the Twitch provider's service config
    public Service Service => _twitchAuthService.Service;

    public static Dictionary<string, string> AvailableScopes => new()
    {
        ["chat:read"] = "Read messages in chat",
        ["chat:edit"] = "Send messages to chat",
        ["channel:moderate"] = "Moderate a channel",
        ["moderator:manage:automod"] = "Manage AutoMod settings"
    };

    public Task<(User, TokenResponse)> Callback(string code)
    {
        // Use the Twitch auth service to handle the OAuth callback
        return _twitchAuthService.Callback(code);
    }

    public async Task<(User, TokenResponse)> ValidateToken(HttpRequest request)
    {
        string authorizationHeader = request.Headers["Authorization"].First() ?? throw new InvalidOperationException();
        string accessToken = authorizationHeader["Bearer ".Length..];
        
        return await ValidateToken(accessToken);
    }

    public async Task<(User, TokenResponse)> ValidateToken(string accessToken)
    {
        // Delegate to the Twitch auth service
        return await _twitchAuthService.ValidateToken(accessToken);
    }

    public async Task<(User, TokenResponse)> RefreshToken(string refreshToken)
    {
        // Delegate to the Twitch auth service
        return await _twitchAuthService.RefreshToken(refreshToken);
    }

    public async Task RevokeToken(string accessToken)
    {
        // Delegate to the Twitch auth service
        await _twitchAuthService.RevokeToken(accessToken);
    }

    public string GetRedirectUrl()
    {
        return _twitchAuthService.GetRedirectUrlWithScopes(AvailableScopes.Keys.ToArray());
    }

    public async Task<DeviceCodeResponse> Authorize(string[]? scopes = null)
    {
        // Use the Twitch auth service for device code flow
        return await _twitchAuthService.Authorize(AvailableScopes.Keys.ToArray());
    }
    
    public async Task<TokenResponse> PollForToken(string deviceCode)
    {
        // Delegate to the Twitch auth service
        return await _twitchAuthService.PollForToken(deviceCode);
    }

    public async Task StoreTokens(TokenResponse tokenResponse)
    {
        // Store the tokens in the BotAccount instead of the Service
        BotAccount? botAccount = await _db.BotAccounts.FirstOrDefaultAsync();
        
        if (botAccount == null)
        {
            // Create a new bot account
            botAccount = new BotAccount
            {
                Username = "BotAccount", // This will be updated when we fetch the user info
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                ClientId = Service.ClientId ?? string.Empty,
                ClientSecret = Service.ClientSecret ?? string.Empty
            };
            
            _db.BotAccounts.Add(botAccount);
        }
        else
        {
            // Update existing bot account
            botAccount.AccessToken = tokenResponse.AccessToken;
            botAccount.RefreshToken = tokenResponse.RefreshToken;
            botAccount.TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        }
        
        await _db.SaveChangesAsync();
        
        // Update username with the Twitch API
        try
        {
            TwitchApiService twitchApiService = _scope.ServiceProvider.GetRequiredService<TwitchApiService>();
            List<UserInfo>? user = await twitchApiService.GetUsers(tokenResponse.AccessToken);
            
            if (user != null && user.Any())
            {
                botAccount.Username = user.First().Login;
                await _db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update bot account username");
        }
    }

    public Task<bool> ConfigureService(ProviderConfigRequest config)
    {
        // Bot service uses the Twitch provider's configuration
        return _twitchAuthService.ConfigureService(config);
    }
}

using NoMercyBot.Database.Models;
using NoMercyBot.Services.Twitch.Dto;

namespace NoMercyBot.Services;

public interface IAuthService
{
    static IConfig Config { get; }

    public Service Service { get; }
    Dictionary<string, string> AvailableScopes => [];
    
    Task<(User, TokenResponse)> Callback(string code);
    Task<(User, TokenResponse)> ValidateToken(string accessToken);
    Task<(User, TokenResponse)> RefreshToken(string refreshToken);
    Task RevokeToken(string accessToken);
    string GetRedirectUrl();
    Task<DeviceCodeResponse> Authorize();
    Task StoreTokens(TokenResponse tokenResponse); 
    Task<bool> ConfigureService(ProviderConfigRequest config);
}
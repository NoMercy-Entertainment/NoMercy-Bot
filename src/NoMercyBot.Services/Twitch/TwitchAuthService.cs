// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeMadeStatic.Global

using System.Collections.Specialized;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Globals.NewtonSoftConverters;
using NoMercyBot.Services.Twitch.Dto;
using RestSharp;

namespace NoMercyBot.Services.Twitch;

public class TwitchAuthService : IAuthService
{
    private readonly IServiceScope _scope;
    private readonly IConfiguration _conf;
    private readonly ILogger<TwitchAuthService> _logger;
    private readonly AppDbContext _db;
    private readonly TwitchApiService _twitchApiService;

    public Service Service => TwitchConfig.Service();
    
    public string ClientId => Service.ClientId ?? throw new InvalidOperationException("Twitch ClientId is not set.");
    private string ClientSecret => Service.ClientSecret ?? throw new InvalidOperationException("Twitch ClientSecret is not set.");
    private string[] Scopes => Service.Scopes ?? throw new InvalidOperationException("Twitch Scopes are not set.");
    
    public TwitchAuthService(IServiceScopeFactory serviceScopeFactory, IConfiguration conf, ILogger<TwitchAuthService> logger, TwitchApiService twitchApiService)
    {
        _scope = serviceScopeFactory.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _conf = conf;
        _logger = logger;
        _twitchApiService = twitchApiService;
    }

    public async Task<(User, TokenResponse)> Callback(string code)
    {        
        RestClient restClient = new(TwitchConfig.AuthUrl);
        
        RestRequest request = new("token", Method.Post);
                    request.AddParameter("client_id", ClientId);
                    request.AddParameter("client_secret", ClientSecret);
                    request.AddParameter("code", code);
                    request.AddParameter("scope", string.Join(' ', Scopes));
                    request.AddParameter("grant_type", "authorization_code");
                    request.AddParameter("redirect_uri", TwitchConfig.RedirectUri);

        RestResponse response = await restClient.ExecuteAsync(request);

        if (!response.IsSuccessful || response.Content is null) 
            throw new(response.Content ?? "Failed to fetch token from Twitch.");
        
        TokenResponse? tokenResponse = response.Content.FromJson<TokenResponse>();
        if (tokenResponse == null) throw new("Invalid response from Twitch.");
        
        User user = await _twitchApiService.FetchUser(tokenResponse);

        return (user, tokenResponse);
    }
    
    public async Task<(User, TokenResponse)> ValidateToken(string accessToken)
    {
        RestClient client = new(TwitchConfig.AuthUrl);
        RestRequest request = new("validate");
                    request.AddHeader("Authorization", $"Bearer {accessToken}");

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to fetch token from Twitch.");
            
        TokenResponse? tokenResponse = response.Content.FromJson<TokenResponse>();
        if (tokenResponse == null) throw new("Invalid response from Twitch.");

        Service service = await _db.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name == Service.Name)
            ?? throw new InvalidOperationException($"Service {Service.Name} not found in database.");

        return (new(), new()
        {
            AccessToken = service.AccessToken,
            RefreshToken = service.RefreshToken,
            ExpiresIn = (int)(service.TokenExpiry - DateTime.UtcNow).Value.TotalSeconds,
        });
    }

    public Task<(User, TokenResponse)> ValidateToken(HttpRequest request)
    {        
        string authorizationHeader = request.Headers["Authorization"].First() ?? throw new InvalidOperationException();
        string accessToken = authorizationHeader["Bearer ".Length..];
        
        return ValidateToken(accessToken);
    }
    
    public async Task<(User, TokenResponse)> RefreshToken(string refreshToken)
    {        
        RestClient client = new(TwitchConfig.AuthUrl);
        
        RestRequest request = new("token", Method.Post);
                    request.AddParameter("client_id", ClientId);
                    request.AddParameter("client_secret", ClientSecret);
                    request.AddParameter("refresh_token", refreshToken);
                    request.AddParameter("grant_type", "refresh_token");

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to fetch token from Twitch.");

        TokenResponse? tokenResponse = response.Content?.FromJson<TokenResponse>();
        if (tokenResponse == null) throw new("Invalid response from Twitch.");

        return (new(), tokenResponse);
    }
    
    public async Task RevokeToken(string accessToken)
    {        
        RestClient client = new(TwitchConfig.AuthUrl);
        
        RestRequest request = new("revoke", Method.Post);
                    request.AddParameter("client_id", ClientId);
                    request.AddParameter("token", accessToken);
                    request.AddParameter("token_type_hint", "access_token");

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to fetch token from Twitch.");
    }
    
    public string GetRedirectUrl()
    {        
        NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
                            query.Add("response_type", "code");
                            query.Add("client_id", ClientId);
                            query.Add("redirect_uri", TwitchConfig.RedirectUri);
                            query.Add("scope", string.Join(' ', Scopes));
        
        UriBuilder uriBuilder = new(TwitchConfig.AuthUrl + "/authorize")
        {
            Query = query.ToString(),
            Scheme = Uri.UriSchemeHttps,
        };
        
        return uriBuilder.ToString();
    }

    public async Task<DeviceCodeResponse> Authorize()
    {        
        RestClient client = new(TwitchConfig.AuthUrl);
        
        RestRequest request = new("device", Method.Post);
                    request.AddParameter("client_id", ClientId);
                    request.AddParameter("scopes", string.Join(' ', Scopes));

        RestResponse response = await client.ExecuteAsync(request);
        
        if (!response.IsSuccessful || response.Content is null) 
            throw new(response.Content ?? "Failed to fetch device code from Twitch.");

        DeviceCodeResponse? deviceCodeResponse = response.Content.FromJson<DeviceCodeResponse>();
        if (deviceCodeResponse == null) throw new("Invalid response from Twitch.");

        return deviceCodeResponse;
    }

    public async Task<TokenResponse> PollForToken(string deviceCode)
    {        
        RestClient restClient = new(TwitchConfig.AuthUrl);
        
        RestRequest request = new("token", Method.Post);
                    request.AddParameter("client_id", ClientId);
                    request.AddParameter("client_secret", ClientSecret);
                    request.AddParameter("grant_type", "urn:ietf:params:oauth:grant-type:device_code");
                    request.AddParameter("device_code", deviceCode);
                    request.AddParameter("scopes", string.Join(' ', Scopes));

        RestResponse response = await restClient.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null) 
            throw new(response.Content ?? "Failed to fetch token from Twitch.");

        TokenResponse? tokenResponse = response.Content.FromJson<TokenResponse>();
        if (tokenResponse == null) throw new("Invalid response from Twitch.");

        return tokenResponse;
    }

    internal async Task<TokenResponse> BotToken()
    {        
        RestClient client = new(TwitchConfig.AuthUrl);
        RestRequest request = new("token", Method.Post);
        request.AddParameter("client_id", ClientId);
        request.AddParameter("client_secret", ClientSecret);
        request.AddParameter("grant_type", "client_credentials");
        request.AddParameter("scope", string.Join(' ', Scopes));

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful) throw new("Failed to fetch bot token.");

        TokenResponse? botToken = response.Content?.FromJson<TokenResponse>();
        if (botToken is null) throw new("Failed to parse bot token.");

        return botToken;
    }
    
    
    public async Task StoreTokens(TokenResponse tokenResponse)
    {
        Service updateService = new()
        {
            Name = Service.Name,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
        };

        AppDbContext dbContext = new();
        await dbContext.Services.Upsert(updateService)
            .On(u => u.Name)
            .WhenMatched((oldUser, newUser) => new()
            {
                AccessToken = newUser.AccessToken,
                RefreshToken = newUser.RefreshToken,
                TokenExpiry = newUser.TokenExpiry
            })
            .RunAsync();
    
        Service.AccessToken = updateService.AccessToken;
        Service.RefreshToken = updateService.RefreshToken;
        Service.TokenExpiry = updateService.TokenExpiry;
    }
}
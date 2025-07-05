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

namespace NoMercyBot.Services.Discord;

public class DiscordAuthService : IAuthService
{
    private readonly IServiceScope _scope;
    private readonly IConfiguration _conf;
    private readonly ILogger<DiscordAuthService> _logger;
    private readonly AppDbContext _db;
    private readonly DiscordApiService _api;
    
    public Service Service => DiscordConfig.Service();
    
    public string ClientId => Service.ClientId ?? throw new InvalidOperationException("Discord ClientId is not set.");
    private string ClientSecret => Service.ClientSecret ?? throw new InvalidOperationException("Discord ClientSecret is not set.");
    private string[] Scopes => Service.Scopes ?? throw new InvalidOperationException("Discord Scopes are not set.");
    
    public DiscordAuthService(IServiceScopeFactory serviceScopeFactory, IConfiguration conf, ILogger<DiscordAuthService> logger, DiscordApiService api)
    {
        _scope = serviceScopeFactory.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _conf = conf;
        _logger = logger;
        _api = api;
    }

    public string GetRedirectUrl()
    {
        NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
        query.Add("response_type", "code");
        query.Add("client_id", DiscordConfig.Service().ClientId);
        query.Add("redirect_uri", DiscordConfig.RedirectUri);
        query.Add("scope", string.Join(' ', DiscordConfig.Service().Scopes));

        UriBuilder uriBuilder = new("https://discord.com/oauth2/authorize")
        {
            Query = query.ToString()
        };

        return uriBuilder.ToString();
    }

    public async Task<(User, TokenResponse)> Callback(string code)
    {
        RestClient client = new(DiscordConfig.ApiUrl);
        
        RestRequest request = new("oauth2/token", Method.Post);
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        request.AddParameter("client_id", DiscordConfig.Service().ClientId);
        request.AddParameter("client_secret", DiscordConfig.Service().ClientSecret);
        request.AddParameter("grant_type", "authorization_code");
        request.AddParameter("code", code);
        request.AddParameter("redirect_uri", DiscordConfig.RedirectUri);

        RestResponse response = await client.ExecuteAsync(request);

        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to fetch token from Discord.");

        TokenResponse? tokenResponse = response.Content.FromJson<TokenResponse>();
        if (tokenResponse == null)
            throw new("Invalid response from Discord.");

        return (new(), tokenResponse);
    }
    
    public async Task<(User, TokenResponse)> ValidateToken(HttpRequest request)
    {
        string authorizationHeader = request.Headers["Authorization"].First() ?? throw new InvalidOperationException();
        string accessToken = authorizationHeader["Bearer ".Length..];
        
        await ValidateToken(accessToken);
        
        return (new(), new()
        {
            AccessToken = accessToken,
            ExpiresIn = 3600,
            RefreshToken = null
        });
    }
    
    public async Task<TokenResponse> ValidateToken(string accessToken)
    {
        RestClient client = new(DiscordConfig.ApiUrl);
        
        RestRequest request = new("users/@me");
        request.AddHeader("Authorization", $"Bearer {accessToken}");

        RestResponse response = await client.ExecuteAsync(request);

        if (!response.IsSuccessful)
            throw new("Invalid access token");

        // Discord doesn't have a dedicated validate endpoint, so we just check if we can access the user's info
        return new()
        {
            AccessToken = accessToken,
        };
    }

    public async Task<(User, TokenResponse)> RefreshToken(string refreshToken)
    {
        RestClient client = new(DiscordConfig.ApiUrl);
        
        RestRequest request = new("oauth2/token", Method.Post);
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        request.AddParameter("client_id", DiscordConfig.Service().ClientId);
        request.AddParameter("client_secret", DiscordConfig.Service().ClientSecret);
        request.AddParameter("grant_type", "refresh_token");
        request.AddParameter("refresh_token", refreshToken);

        RestResponse response = await client.ExecuteAsync(request);

        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to refresh token from Discord.");

        TokenResponse? tokenResponse = response.Content.FromJson<TokenResponse>();
        if (tokenResponse == null)
            throw new("Invalid response from Discord.");

        return (new(), tokenResponse);
    }

    public async Task RevokeToken(string accessToken)
    {
        RestClient client = new(DiscordConfig.ApiUrl);
        
        RestRequest request = new("oauth2/token/revoke", Method.Post);
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        request.AddParameter("client_id", DiscordConfig.Service().ClientId);
        request.AddParameter("client_secret", DiscordConfig.Service().ClientSecret);
        request.AddParameter("token", accessToken);
        request.AddParameter("token_type_hint", "access_token");

        RestResponse response = await client.ExecuteAsync(request);

        if (!response.IsSuccessful)
            throw new("Failed to revoke token from Discord.");
    }

    public Task<DeviceCodeResponse> Authorize()
    {
        // Discord doesn't support device code flow
        throw new NotImplementedException("Discord doesn't support device code flow");
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
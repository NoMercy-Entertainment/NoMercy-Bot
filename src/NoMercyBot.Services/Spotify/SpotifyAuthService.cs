﻿using System.Collections.Specialized;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Globals.NewtonSoftConverters;
using NoMercyBot.Services.Interfaces;
using NoMercyBot.Services.Twitch.Dto;
using RestSharp;

namespace NoMercyBot.Services.Spotify;

public class SpotifyAuthService : IAuthService
{
    private readonly IConfiguration _conf;
    private readonly ILogger<SpotifyAuthService> _logger;
    private readonly IServiceScope _scope;
    private readonly AppDbContext _dbContext;
    private readonly SpotifyApiService _api;

    public Service Service => SpotifyConfig.Service();
    
    public string ClientId => Service.ClientId ?? throw new InvalidOperationException("Spotify ClientId is not set.");
    private string ClientSecret => Service.ClientSecret ?? throw new InvalidOperationException("Spotify ClientSecret is not set.");
    private string[] Scopes => Service.Scopes ?? throw new InvalidOperationException("Spotify Scopes are not set.");
    public string UserId => Service.UserId ?? throw new InvalidOperationException("Spotify UserId is not set.");
    public string UserName => Service.UserName ?? throw new InvalidOperationException("Spotify UserName is not set.");
    public Dictionary<string, string> AvailableScopes => SpotifyConfig.AvailableScopes ?? throw new InvalidOperationException("Spotify Scopes are not set.");
    
    public SpotifyAuthService(IServiceScopeFactory serviceScopeFactory, IConfiguration conf, ILogger<SpotifyAuthService> logger, SpotifyApiService api)
    {
        _scope = serviceScopeFactory.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _conf = conf;
        _logger = logger;
        _api = api;
    }

    public string GetRedirectUrl()
    {
        NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
        query.Add("response_type", "code");
        query.Add("client_id", ClientId);
        query.Add("redirect_uri", SpotifyConfig.RedirectUri);
        query.Add("scope", string.Join(' ', Scopes));
        
        UriBuilder uriBuilder = new("https://accounts.spotify.com/authorize")
        {
            Query = query.ToString()
        };
        
        return uriBuilder.ToString();
    }

    public async Task<(User, TokenResponse)> Callback(string code)
    {
        RestClient client = new(SpotifyConfig.AuthUrl);
        
        RestRequest request = new("token", Method.Post);
        request.AddHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}")));
        request.AddParameter("grant_type", "authorization_code");
        request.AddParameter("code", code);
        request.AddParameter("redirect_uri", SpotifyConfig.RedirectUri);

        RestResponse response = await client.ExecuteAsync(request);
        
        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to fetch token from Spotify.");
        
        TokenResponse? tokenResponse = response.Content.FromJson<TokenResponse>();
        if (tokenResponse == null) 
            throw new("Invalid response from Spotify.");
        
        await StoreTokens(tokenResponse, new()
        {
            Id = "",
            Username = ""
        });

        return (new(), tokenResponse);
    }

    public Task<(User, TokenResponse)> ValidateToken(HttpRequest request)
    {
        string authorizationHeader = request.Headers["Authorization"].First() ?? throw new InvalidOperationException();
        string accessToken = authorizationHeader["Bearer ".Length..];
        
        return ValidateToken(accessToken);
    }
    
    public async Task<(User, TokenResponse)> ValidateToken(string accessToken)
    {
        RestClient client = new(SpotifyConfig.ApiUrl);
        
        RestRequest request = new("v1/me");
        request.AddHeader("Authorization", $"Bearer {accessToken}");

        RestResponse response = await client.ExecuteAsync(request);
        
        if (!response.IsSuccessful)
            throw new("Invalid access token");
        
        return (new(), new()
        {
            AccessToken = accessToken,
            RefreshToken = Service.RefreshToken,
            ExpiresIn = (int)(Service.TokenExpiry - DateTime.UtcNow).Value.TotalSeconds
        });
    }

    public async Task<(User, TokenResponse)> RefreshToken(string refreshToken)
    {
        RestClient client = new(SpotifyConfig.AuthUrl);
        
        RestRequest request = new("token", Method.Post);
        request.AddHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}")));
        request.AddParameter("grant_type", "refresh_token");
        request.AddParameter("refresh_token", refreshToken);

        RestResponse response = await client.ExecuteAsync(request);
        
        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to refresh token from Spotify.");
        
        TokenResponse? tokenResponse = response.Content.FromJson<TokenResponse>();
        if (tokenResponse == null) 
            throw new("Invalid response from Spotify.");

        // Spotify might not return a refresh token if it's still valid, so we need to keep the old one
        if (string.IsNullOrEmpty(tokenResponse.RefreshToken))
            tokenResponse.RefreshToken = refreshToken;

        return (new(), tokenResponse);
    }

    public Task RevokeToken(string accessToken)
    {
        // Spotify doesn't have a revoke endpoint - tokens will expire naturally
        // Just return completed task for interface compatibility
        return Task.CompletedTask;
    }

    public Task<DeviceCodeResponse> Authorize(string[]? scopes = null)
    {
        // Spotify doesn't support device code flow in the same way as Twitch
        throw new NotImplementedException("Spotify doesn't support device code flow");
    }
    
    public async Task StoreTokens(TokenResponse tokenResponse, User user)
    {
        Service updateService = new()
        {
            Name = Service.Name,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            UserId = user.Id,
            UserName = user.Username
        };

        await _dbContext.Services.Upsert(updateService)
            .On(u => u.Name)
            .WhenMatched((oldUser, newUser) => new()
            {
                AccessToken = newUser.AccessToken,
                RefreshToken = newUser.RefreshToken,
                TokenExpiry = newUser.TokenExpiry,
                UserId = newUser.UserId,
                UserName = newUser.UserName,
            })
            .RunAsync();
    
        Service.AccessToken = updateService.AccessToken;
        Service.RefreshToken = updateService.RefreshToken;
        Service.TokenExpiry = updateService.TokenExpiry;
        Service.UserId = updateService.UserId;
        Service.UserName = updateService.UserName;
    }

    public Task<bool> ConfigureService(ProviderConfigRequest config)
    {
        throw new NotImplementedException();
    }
}
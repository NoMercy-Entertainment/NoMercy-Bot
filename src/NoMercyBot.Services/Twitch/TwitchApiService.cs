// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeMadeStatic.Global

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime.TimeZones;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Globals.NewtonSoftConverters;
using NoMercyBot.Services.Twitch.Dto;
using RestSharp;
using TwitchLib.Api.Helix.Models.Chat.GetUserChatColor;

namespace NoMercyBot.Services.Twitch;

public class TwitchApiService
{
    private readonly IConfiguration _conf;
    private readonly ILogger<TwitchApiService> _logger;

    public Service Service => TwitchConfig.Service();
    
    public string ClientId => Service.ClientId ?? throw new InvalidOperationException("Twitch ClientId is not set.");
    
    public TwitchApiService(IConfiguration conf, ILogger<TwitchApiService> logger)
    {
        _conf = conf;
        _logger = logger;
    }
        
    public async Task<List<UserInfo>?> GetUsers(string accessToken, string[]? userIds = null, string? userId = null)
    {        
        if (string.IsNullOrEmpty(accessToken)) throw new("No access token provided.");
        if (userIds is not null && userIds.Length == 0) throw new($"userIds must contain at least 1 userId");
        if (userIds is not null && userIds.Length > 100) throw new("Too many user ids provided.");
        
        RestClient client = new(TwitchConfig.ApiUrl);
        RestRequest request = new("users");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddHeader("Client-Id", TwitchConfig.Service().ClientId!);
        
        foreach (string id in userIds ?? [])
        {
            request.AddQueryParameter("user_id", id);
        }
        
        if(userId != null) request.AddQueryParameter("id", userId);
        
        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to fetch user information.");

        UserInfoResponse? userInfoResponse = response.Content?.FromJson<UserInfoResponse>();
        if (userInfoResponse is null) throw new("Failed to parse user information.");
        
        return userInfoResponse.Data;
    }
    
    public async Task<User> FetchUser(TokenResponse tokenResponse, string? countryCode = null, string? id = null)
    {        
        List<UserInfo>? users = await GetUsers(accessToken: tokenResponse.AccessToken, userId: id);
        if (users is null || users.Count == 0) throw new("Failed to fetch user information.");
        
        UserInfo userInfo = users.First();
        
        IEnumerable<string>? zoneIds = TzdbDateTimeZoneSource.Default.ZoneLocations?
            .Where(x => x.CountryCode == countryCode)
            .Select(x => x.ZoneId)
            .ToList();

        User user = new()
        {
            Id = userInfo.Id,
            Username = userInfo.Login,
            DisplayName = userInfo.DisplayName,
            Description = userInfo.Description,
            ProfileImageUrl = userInfo.ProfileImageUrl,
            OfflineImageUrl = userInfo.OfflineImageUrl,
            BroadcasterType = userInfo.BroadcasterType,
            Timezone = zoneIds?.FirstOrDefault(),
        };
        
        GetUserChatColorResponse? colors = await GetUserChatColors(tokenResponse, [userInfo.Id]);
        
        string? color = colors?.Data.First().Color;
        
        user.Color = string.IsNullOrEmpty(color)
            ? "#9146FF"
            : color;

        AppDbContext dbContext = new();
        await dbContext.Users.Upsert(user)
            .On(u => u.Id)
            .WhenMatched((oldUser, newUser) => new()
            {
                Username = newUser.Username,
                DisplayName = newUser.DisplayName,
                ProfileImageUrl = newUser.ProfileImageUrl,
                OfflineImageUrl = newUser.OfflineImageUrl,
                Color = newUser.Color,
                BroadcasterType = newUser.BroadcasterType,
            })
            .RunAsync();
        
        return user;
    }

    public async Task<GetUserChatColorResponse?> GetUserChatColors(TokenResponse tokenResponse, string[] userIds)
    {        
        if (userIds.Any(string.IsNullOrEmpty)) throw new("Invalid user id provided.");
        if (userIds.Length == 0) throw new($"userIds must contain at least 1 userId");
        if (userIds.Length > 100) throw new("Too many user ids provided.");
        
        RestClient client = new(TwitchConfig.ApiUrl);
        RestRequest request = new($"chat/color");
        request.AddHeader("Authorization", $"Bearer {tokenResponse.AccessToken}");
        request.AddHeader("Client-Id", TwitchConfig.Service().ClientId!);

        foreach (string id in userIds)
        {
            request.AddQueryParameter("user_id", id);
        }

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to fetch user color.");
        
        GetUserChatColorResponse? colors = response.Content?.FromJson<GetUserChatColorResponse>();
        if (colors is null) throw new("Failed to parse user chat color.");
        
        return colors;
    }
    
    public async Task<ChannelResponse> GetUserModeration(string accessToken, string userId)
    {        
        if (string.IsNullOrEmpty(accessToken)) throw new("No access token provided.");
        if (string.IsNullOrEmpty(userId)) throw new("No user id provided.");
        
        RestClient client = new(TwitchConfig.ApiUrl);
        
        RestRequest request = new("moderation/channels");
                    request.AddHeader("Authorization", $"Bearer {accessToken}");
                    request.AddHeader("client-id", TwitchConfig.Service().ClientId!);
                    request.AddParameter("user_id", userId);

        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to fetch user information.");
        
        ChannelResponse? channelResponse = response.Content.FromJson<ChannelResponse>();
        if (channelResponse == null) throw new("Invalid response from Twitch.");
        
        return channelResponse;
    }

    public async Task<string?> CreateEventSubSubscription(string channelChatMessage, string p1, Dictionary<string, string> messageCondition, string callbackUrl, string? appAccessToken)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAllEventSubSubscriptions(string? accessToken)
    {
        throw new NotImplementedException();
    }
}
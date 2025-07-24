// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeMadeStatic.Global

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime.TimeZones;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Globals.Information;
using NoMercyBot.Globals.NewtonSoftConverters;
using NoMercyBot.Services.Other;
using NoMercyBot.Services.Twitch.Dto;
using RestSharp;
using TwitchLib.Api.Helix.Models.Chat.GetUserChatColor;

namespace NoMercyBot.Services.Twitch;

public class TwitchApiService
{
    private readonly IConfiguration _conf;
    private readonly IServiceScope _scope;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<TwitchApiService> _logger;
    private readonly PronounService _pronounService;

    public Service Service => TwitchConfig.Service();
    
    public string ClientId => Service.ClientId ?? throw new InvalidOperationException("Twitch ClientId is not set.");
    
    public TwitchApiService(IServiceScopeFactory serviceScopeFactory, IConfiguration conf, ILogger<TwitchApiService> logger, PronounService pronounService)
    {
        _scope = serviceScopeFactory.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _conf = conf;
        _logger = logger;
        _pronounService = pronounService;
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
        if (userInfoResponse?.Data is null) throw new("Failed to parse user information.");
        
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
        
        GetUserChatColorResponse? colors = await GetUserChatColors(tokenResponse, [userInfo.Id]);
        Pronoun? pronoun = await _pronounService.GetUserPronoun(userInfo.Login);

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
            Pronoun = pronoun,
        };
        
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
                UpdatedAt = DateTime.UtcNow,
            })
            .RunAsync();
        
        ChannelInfo? channelInfo = await FetchChannelInfo(tokenResponse.AccessToken, userInfo.Id);
        if (channelInfo is not null)
        {
            await dbContext.ChannelInfo.Upsert(channelInfo)
                .On(c => c.Id)
                .WhenMatched((oldChannel, newChannel) => new()
                {
                    Language = newChannel.Language,
                    GameId = newChannel.GameId,
                    GameName = newChannel.GameName,
                    Title = newChannel.Title,
                    Delay = newChannel.Delay,
                    Tags = newChannel.Tags,
                    ContentLabels = newChannel.ContentLabels,
                    IsBrandedContent = newChannel.IsBrandedContent,
                    UpdatedAt = DateTime.UtcNow,
                })
                .RunAsync();
        }
        
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
    
    private async Task<ChannelInfo?> FetchChannelInfo(string accessToken, string broadcasterId)
    {
        RestClient client = new(TwitchConfig.ApiUrl);
        
        RestRequest request = new($"channels?broadcaster_id={broadcasterId}");
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("Client-Id", TwitchConfig.Service().ClientId!);
            
        RestResponse response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || response.Content is null)
            throw new(response.Content ?? "Failed to fetch channel information.");
        
        ChannelInfoResponse? channelInfoResponse = response.Content.FromJson<ChannelInfoResponse>();
        if (channelInfoResponse == null || channelInfoResponse.Data.Count == 0) 
            throw new("Invalid response from Twitch or no channel information found.");

        ChannelInfoDto? dto = channelInfoResponse?.Data.FirstOrDefault();
        if (dto == null) return null;

        return new()
        {
            Id = dto.BroadcasterId,
            Language = dto.Language,
            GameId = dto.GameId,
            GameName = dto.GameName,
            Title = dto.Title,
            Delay = dto.Delay,
            Tags = dto.Tags,
            ContentLabels = dto.ContentLabels,
            IsBrandedContent = dto.IsBrandedContent
        };
    }

    public async Task<string?> CreateEventSubSubscription(string eventType, string version, Dictionary<string, string> conditions, string callbackUrl, string? accessToken)
    {
        if (string.IsNullOrEmpty(accessToken)) throw new("No access token provided.");
        
        try
        {
            RestClient client = new(TwitchConfig.ApiUrl);
            RestRequest request = new("eventsub/subscriptions", Method.Post);
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("Client-Id", TwitchConfig.Service().ClientId!);
            request.AddHeader("Content-Type", "application/json");
            
            var subscription = new
            {
                type = eventType,
                version = version,
                condition = conditions,
                transport = new
                {
                    method = "webhook",
                    callback = callbackUrl,
                    secret = EventSubSecretStore.Secret
                }
            };
            
            _logger.LogInformation("Creating EventSub subscription: Type={EventType}, Version={Version}, Callback={Callback}, Conditions={@Conditions}", 
                eventType, version, callbackUrl, conditions);
            
            request.AddJsonBody(subscription);
            
            RestResponse response = await client.ExecuteAsync(request);
            
            if (!response.IsSuccessful || response.Content is null)
            {
                _logger.LogError("Failed to create EventSub subscription: Status={StatusCode}, Content={Content}", 
                    (int)response.StatusCode, response.Content);
                return null;
            }
            
            _logger.LogInformation("EventSub subscription response: {Content}", response.Content);
            
            // Parse the response to get the subscription ID
            dynamic? responseObject = System.Text.Json.JsonSerializer.Deserialize<dynamic>(response.Content);
            string? subscriptionId = responseObject?.data?[0]?.id?.ToString();
            
            if (subscriptionId != null)
            {
                _logger.LogInformation("Successfully created EventSub subscription: ID={SubscriptionId}", subscriptionId);
            }
            else
            {
                _logger.LogWarning("Created EventSub subscription but couldn't extract ID from response");
            }
            
            return subscriptionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating EventSub subscription");
            return null;
        }
    }

    public async Task DeleteEventSubSubscription(string subscriptionId, string? accessToken)
    {
        if (string.IsNullOrEmpty(accessToken)) throw new("No access token provided.");
        
        try
        {
            RestClient client = new(TwitchConfig.ApiUrl);
            RestRequest request = new($"eventsub/subscriptions?id={subscriptionId}", Method.Delete);
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("Client-Id", TwitchConfig.Service().ClientId!);
            
            RestResponse response = await client.ExecuteAsync(request);
            
            if (!response.IsSuccessful)
            {
                _logger.LogError($"Failed to delete EventSub subscription: {response.Content}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting EventSub subscription {subscriptionId}");
        }
    }

    public async Task DeleteAllEventSubSubscriptions(string? accessToken)
    {
        if (string.IsNullOrEmpty(accessToken)) throw new("No access token provided.");
        
        try
        {
            RestClient client = new(TwitchConfig.ApiUrl);
            RestRequest request = new("eventsub/subscriptions", Method.Get);
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("Client-Id", TwitchConfig.Service().ClientId!);
            
            RestResponse response = await client.ExecuteAsync(request);
            
            if (!response.IsSuccessful || response.Content is null)
            {
                _logger.LogError($"Failed to fetch EventSub subscriptions: {response.Content}");
                return;
            }
            
            // Parse the response to get all subscription IDs
            dynamic? responseObject = System.Text.Json.JsonSerializer.Deserialize<dynamic>(response.Content);
            dynamic? subscriptions = responseObject?.data?.EnumerateArray();
            
            if (subscriptions != null)
            {
                foreach (dynamic? subscription in subscriptions)
                {
                    string? id = subscription.GetProperty("id").ToString();
                    if (!string.IsNullOrEmpty(id))
                    {
                        await DeleteEventSubSubscription(id, accessToken);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all EventSub subscriptions");
        }
    }
}
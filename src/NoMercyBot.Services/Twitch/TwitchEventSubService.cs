using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Globals.Information;
using NoMercyBot.Globals.NewtonSoftConverters;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace NoMercyBot.Services.Twitch;

public class TwitchEventSubService : IEventSubService
{
    private readonly ILogger<TwitchEventSubService> _logger;
    private readonly AppDbContext _dbContext;
    private readonly TwitchApiService _twitchApiService;
    
    public string ProviderName => "twitch";
    
    private readonly Dictionary<string, string> _availableEventTypes = new()
    {
        { "channel.update", "When a broadcaster updates their channel properties" },
        { "channel.follow", "When a user follows a broadcaster's channel" },
        { "channel.subscribe", "When a user subscribes to a broadcaster's channel" },
        { "channel.subscription.gift", "When a gift subscription is received" },
        { "channel.subscription.message", "When a subscriber shares a re-subscription message" },
        { "channel.cheer", "When a user cheers bits on a broadcaster's channel" },
        { "channel.raid", "When a broadcaster raids another broadcaster's channel" },
        { "channel.chat.message", "When a user sends a message to a channel's chat" },
        { "stream.online", "When a broadcaster starts a stream" },
        { "stream.offline", "When a broadcaster stops a stream" }
    };
    
    public TwitchEventSubService(
        ILogger<TwitchEventSubService> logger,
        AppDbContext dbContext,
        TwitchApiService twitchApiService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _twitchApiService = twitchApiService;
    }
    
    public bool VerifySignature(HttpRequest request, string payload)
    {
        try
        {
            if (!request.Headers.TryGetValue("Twitch-Eventsub-Message-Signature", out StringValues signatureValues))
                return false;
                
            if (!request.Headers.TryGetValue("Twitch-Eventsub-Message-Id", out StringValues messageIdValues))
                return false;
                
            if (!request.Headers.TryGetValue("Twitch-Eventsub-Message-Timestamp", out StringValues timestampValues))
                return false;
                
            string signature = signatureValues.ToString();
            string messageId = messageIdValues.ToString();
            string timestamp = timestampValues.ToString();
            
            string hmacMessage = messageId + timestamp + payload;
            string expectedSignature = "sha256=" + ComputeHmac256(EventSubSecretStore.Secret, hmacMessage);
            
            return signature == expectedSignature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Twitch signature");
            return false;
        }
    }
    
    public async Task<IActionResult> HandleEventAsync(HttpRequest request, string payload, string eventType)
    {
        try
        {
            if (!request.Headers.TryGetValue("Twitch-Eventsub-Message-Type", out StringValues messageTypeValues))
                return new BadRequestObjectResult("Missing message type header");
                
            string messageType = messageTypeValues.ToString();
            
            switch (messageType)
            {
                case "webhook_callback_verification":
                    JObject? challengeJson = payload.FromJson<JObject>();
                    string challenge = challengeJson?["challenge"]?.ToString() ?? string.Empty;
                    _logger.LogInformation("Received verification request with challenge: {Challenge}", challenge);
                    return new OkObjectResult(challenge);
                    
                case "notification":
                    await ProcessEventNotification(payload, eventType);
                    return new OkResult();
                    
                case "revocation":
                    _logger.LogWarning("Subscription revoked: {Payload}", payload);
                    // Handle revocation by updating DB
                    JObject? revocationJson = payload.FromJson<JObject>();
                    string? subscriptionId = revocationJson?["subscription"]?["id"]?.ToString();
                    if (subscriptionId != null)
                    {
                        EventSubscription? sub = await _dbContext.EventSubscriptions
                            .FirstOrDefaultAsync(s => s.Provider == ProviderName && s.SubscriptionId == subscriptionId);
                            
                        if (sub != null)
                        {
                            sub.Enabled = false;
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                    return new OkResult();
                    
                default:
                    _logger.LogWarning("Unknown message type: {MessageType}", messageType);
                    return new BadRequestObjectResult($"Unknown message type: {messageType}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Twitch event");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    private async Task ProcessEventNotification(string payload, string eventType)
    {
        try
        {
            // Parse the notification
            JObject? notification = payload.FromJson<JObject>();
            if (notification == null)
            {
                _logger.LogError("Failed to parse notification payload");
                return;
            }
            
            string? subscriptionType = notification["subscription"]?["type"]?.ToString();
            
            if (string.IsNullOrEmpty(subscriptionType))
            {
                _logger.LogError("Missing subscription type in notification");
                return;
            }
            
            // Check if this event type is enabled
            EventSubscription? subscription = await _dbContext.EventSubscriptions
                .FirstOrDefaultAsync(s => s.Provider == ProviderName && s.EventType == subscriptionType);
                
            if (subscription == null || !subscription.Enabled)
            {
                _logger.LogInformation("Ignoring disabled event: {EventType}", subscriptionType);
                return;
            }
            
            // Handle different event types
            switch (subscriptionType)
            {
                case "stream.online":
                    await HandleStreamOnline(notification);
                    break;
                    
                case "stream.offline":
                    await HandleStreamOffline(notification);
                    break;
                    
                case "channel.chat.message":
                    // Process chat message
                    _logger.LogInformation("Received chat message event");
                    break;
                    
                // Add more cases for other event types
                    
                default:
                    _logger.LogInformation("Received {EventType} event: {Payload}", subscriptionType, payload);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Twitch notification");
        }
    }
    
    private async Task HandleStreamOnline(JObject notification)
    {
        try
        {
            string? broadcasterId = notification["payload"]?["event"]?["broadcaster_user_id"]?.ToString();
            
            if (string.IsNullOrEmpty(broadcasterId))
                return;
                
            Channel? channel = await _dbContext.Channels
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == broadcasterId);
                
            if (channel != null)
            {
                channel.User.IsLive = true;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Updated stream status to online for {ChannelId}", broadcasterId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling stream.online event");
        }
    }
    
    private async Task HandleStreamOffline(JObject notification)
    {
        try
        {
            string? broadcasterId = notification["payload"]?["event"]?["broadcaster_user_id"]?.ToString();
            
            if (string.IsNullOrEmpty(broadcasterId))
                return;
                
            Channel? channel = await _dbContext.Channels
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == broadcasterId);
                
            if (channel != null)
            {
                channel.User.IsLive = false;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Updated stream status to offline for {ChannelId}", broadcasterId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling stream.offline event");
        }
    }
    
    public async Task<List<EventSubscription>> GetAllSubscriptionsAsync()
    {
        return await _dbContext.EventSubscriptions
            .Where(s => s.Provider == ProviderName)
            .ToListAsync();
    }
    
    public async Task<EventSubscription?> GetSubscriptionAsync(string id)
    {
        return await _dbContext.EventSubscriptions
            .FirstOrDefaultAsync(s => s.Provider == ProviderName && s.Id == id);
    }
    
    public async Task<EventSubscription> CreateSubscriptionAsync(string eventType, bool enabled = true)
    {
        // Check if event type is valid
        if (!_availableEventTypes.ContainsKey(eventType))
            throw new ArgumentException($"Invalid event type: {eventType}");
            
        // Check if subscription already exists
        EventSubscription? existingSub = await _dbContext.EventSubscriptions
            .FirstOrDefaultAsync(s => s.Provider == ProviderName && s.EventType == eventType);
            
        if (existingSub != null)
        {
            existingSub.Enabled = enabled;
            existingSub.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return existingSub;
        }
        
        // Create new subscription
        EventSubscription subscription = new(ProviderName, eventType, enabled)
        {
            CallbackUrl = TwitchConfig.EventSubCallbackUri
        };
        
        if (TwitchConfig.Service().AccessToken != null)
        {
            // Register with Twitch API (simplified, would need to be adjusted based on event type)
            Dictionary<string, string> conditions = new();
            
            // Configure conditions based on event type
            if (eventType.StartsWith("channel."))
            {
                conditions["broadcaster_user_id"] = TwitchConfig.Service().Name;
            }
            
            string? subscriptionId = await _twitchApiService.CreateEventSubSubscription(
                eventType,
                "1", // version
                conditions,
                subscription.CallbackUrl,
                TwitchConfig.Service().AccessToken);
                
            if (subscriptionId != null)
            {
                subscription.SubscriptionId = subscriptionId;
                subscription.ExpiresAt = DateTime.UtcNow.AddDays(1); // Most Twitch subs expire after 1 day
            }
        }
        
        await _dbContext.EventSubscriptions.AddAsync(subscription);
        await _dbContext.SaveChangesAsync();
        
        return subscription;
    }
    
    public async Task UpdateSubscriptionAsync(string id, bool enabled)
    {
        EventSubscription? subscription = await _dbContext.EventSubscriptions
            .FirstOrDefaultAsync(s => s.Provider == ProviderName && s.Id == id);
            
        if (subscription == null)
            throw new KeyNotFoundException($"Subscription not found: {id}");
            
        subscription.Enabled = enabled;
        subscription.UpdatedAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task DeleteSubscriptionAsync(string id)
    {
        EventSubscription? subscription = await _dbContext.EventSubscriptions
            .FirstOrDefaultAsync(s => s.Provider == ProviderName && s.Id == id);
            
        if (subscription == null)
            return;
            
        // If we have a subscription ID from Twitch, delete it there too
        if (!string.IsNullOrEmpty(subscription.SubscriptionId) && TwitchConfig.Service().AccessToken != null)
        {
            // Call Twitch API to delete the subscription
            try
            {
                // This would need the implementation in TwitchApiService
                // await _twitchApiService.DeleteEventSubSubscription(subscription.SubscriptionId, TwitchConfig.Service().AccessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete Twitch subscription {Id}", subscription.SubscriptionId);
            }
        }
        
        _dbContext.EventSubscriptions.Remove(subscription);
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task<bool> DeleteAllSubscriptionsAsync()
    {
        try
        {
            // Get all subscriptions for this provider
            List<EventSubscription> subscriptions = await _dbContext.EventSubscriptions
                .Where(s => s.Provider == ProviderName)
                .ToListAsync();
                
            // Delete from Twitch API if possible
            if (TwitchConfig.Service().AccessToken != null)
            {
                try
                {
                    await _twitchApiService.DeleteAllEventSubSubscriptions(TwitchConfig.Service().AccessToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete all Twitch subscriptions");
                }
            }
            
            // Delete from our database
            _dbContext.EventSubscriptions.RemoveRange(subscriptions);
            await _dbContext.SaveChangesAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all subscriptions");
            return false;
        }
    }
    
    public IEnumerable<string> GetAvailableEventTypes()
    {
        return _availableEventTypes.Keys;
    }
    
    private static string ComputeHmac256(string secretKey, string message)
    {
        byte[] secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

        using (HMACSHA256 hmac = new(secretKeyBytes))
        {
            byte[] hashBytes = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}

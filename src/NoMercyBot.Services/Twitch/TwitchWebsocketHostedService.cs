using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using TwitchLib.Api;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Stream;
using TwitchLib.EventSub.Websockets.Core.EventArgs.User;

namespace NoMercyBot.Services.Twitch;

public class TwitchWebsocketHostedService : IHostedService
{
    private readonly AppDbContext _dbContext;
    private readonly EventSubWebsocketClient _eventSubWebsocketClient;
    private readonly ILogger<TwitchWebsocketHostedService> _logger;
    private CancellationTokenSource _cts = new();
    private readonly TwitchAPI _twitchApi = new();
    private readonly TwitchApiService _twitchApiService;
    private readonly Dictionary<string, string> _channelSubscriptionIds = new();

    public TwitchWebsocketHostedService(AppDbContext dbContext, ILogger<TwitchWebsocketHostedService> logger, EventSubWebsocketClient eventSubWebsocketClient, TwitchApiService twitchApiService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _eventSubWebsocketClient = eventSubWebsocketClient;
        _twitchApiService = twitchApiService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TwitchWebsocketHostedService starting.");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _eventSubWebsocketClient.WebsocketConnected += OnConnected;
        _eventSubWebsocketClient.WebsocketDisconnected += OnDisconnected;
        _eventSubWebsocketClient.WebsocketReconnected += OnReconnected;
        _eventSubWebsocketClient.ErrorOccurred += OnError;
        _eventSubWebsocketClient.UserUpdate += OnServiceAnnouncement;

        _eventSubWebsocketClient.ChannelFollow += OnChannelFollow;
        _eventSubWebsocketClient.ChannelUpdate += OnChannelUpdate;
        _eventSubWebsocketClient.StreamOnline += OnStreamOnline;
        _eventSubWebsocketClient.StreamOffline += OnStreamOffline;
        _eventSubWebsocketClient.ChannelChatMessage += OnChannelChatMessage;

        _twitchApi.Settings.ClientId = TwitchConfig.Service().ClientId;
        _twitchApi.Settings.AccessToken = TwitchConfig.Service().AccessToken;
        
        await _eventSubWebsocketClient.ConnectAsync();

        // Subscribe to events for all channels
        await SubscribeToAllChannels(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TwitchWebsocketHostedService stopping.");
        await _cts.CancelAsync();

        // Unsubscribe from all events
        await UnsubscribeFromAllChannels();

        await _eventSubWebsocketClient.DisconnectAsync();
    }

    private async Task OnConnected(object sender, WebsocketConnectedArgs e)
    {
        _logger.LogInformation("WebSocket connected.");

        await Task.CompletedTask;
    }

    private async Task OnDisconnected(object sender, EventArgs args)
    {
        _logger.LogInformation("WebSocket disconnected.");
        
        await Task.CompletedTask;
    }

    private async Task OnReconnected(object sender, EventArgs e)
    {
        _logger.LogInformation("WebSocket reconnected. Resubscribing to events.");
        await SubscribeToAllChannels(_cts.Token);
    }

    private async Task OnError(object sender, ErrorOccuredArgs e)
    {
        _logger.LogError($"WebSocket error: {e.Exception.Message}");
        await Task.CompletedTask;
    }

    private async Task OnServiceAnnouncement(object sender, UserUpdateArgs e)
    {
        _logger.LogInformation($"Service announcement: {e.Notification.Payload.Event.UserLogin}");
        await Task.CompletedTask;
    }

    private async Task OnChannelFollow(object sender, ChannelFollowArgs e)
    {
        _logger.LogInformation($"New follower: {e.Notification.Payload.Event.UserLogin} in channel {e.Notification.Payload.Event.BroadcasterUserLogin}");
        await Task.CompletedTask;
    }

    private async Task OnChannelUpdate(object sender, ChannelUpdateArgs e)
    {
        _logger.LogInformation($"Channel updated: {e.Notification.Payload.Event.BroadcasterUserLogin}");
        await Task.CompletedTask;
    }

    private async Task OnStreamOnline(object sender, StreamOnlineArgs e)
    {
        _logger.LogInformation($"Stream online: {e.Notification.Payload.Event.BroadcasterUserLogin}");

        Channel? channel = await _dbContext.Channels
            .Include(channel => channel.User)
            .FirstOrDefaultAsync(c => c.Id == e.Notification.Payload.Event.BroadcasterUserId, _cts.Token);
        
        if (channel != null)
        {
            channel.User.IsLive = true;
            await _dbContext.SaveChangesAsync(_cts.Token);
        }
    }

    private async Task OnStreamOffline(object sender, StreamOfflineArgs e)
    {
        _logger.LogInformation($"Stream offline: {e.Notification.Payload.Event.BroadcasterUserLogin}");

        Channel? channel = await _dbContext.Channels
            .Include(channel => channel.User)
            .FirstOrDefaultAsync(c => c.Id == e.Notification.Payload.Event.BroadcasterUserId, _cts.Token);
        
        if (channel != null)
        {
            channel.User.IsLive = false;
            await _dbContext.SaveChangesAsync(_cts.Token);
        }
    }

    private async Task OnChannelChatMessage(object sender, ChannelChatMessageArgs e)
    {
        _logger.LogInformation($"Message received from {e.Notification.Payload.Event.ChatterUserName} in channel {e.Notification.Payload.Event.BroadcasterUserLogin}: {e.Notification.Payload.Event.Message.Text}");
        await Task.CompletedTask;
    }

    private async Task SubscribeToAllChannels(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Subscribing to events for all channels...");
        List<Channel> channels = await _dbContext.Channels.ToListAsync(cancellationToken);

        foreach (Channel channel in channels)
        {
            await SubscribeToStreamEvents(channel.Id);
        }
    }

    private async Task SubscribeToStreamEvents(string channelId)
    {
        string appAccessToken = TwitchConfig.Service().AccessToken;
        string callbackUrl = TwitchConfig.EventSubCallbackUri;

        // Stream Online
        Dictionary<string, string> onlineCondition = new()
        {
            { "broadcaster_user_id", channelId }
        };
        
        string? onlineSubscriptionId = await _twitchApiService.CreateEventSubSubscription(
            "stream.online", "1",
            onlineCondition, callbackUrl, appAccessToken);

        if (onlineSubscriptionId != null)
        {
            _channelSubscriptionIds[$"online_{channelId}"] = onlineSubscriptionId;
            // _logger.LogInformation($"Subscribed to stream.online for channel {channelId} with subscription ID {onlineSubscriptionId}");
        }

        // Stream Offline
        Dictionary<string, string> offlineCondition = new()
        {
            { "broadcaster_user_id", channelId }
        };
        
        string? offlineSubscriptionId = await _twitchApiService.CreateEventSubSubscription(
            "stream.offline", "1",
            offlineCondition, callbackUrl, appAccessToken);

        if (offlineSubscriptionId != null)
        {
            _channelSubscriptionIds[$"offline_{channelId}"] = offlineSubscriptionId;
            // _logger.LogInformation($"Subscribed to stream.offline for channel {channelId} with subscription ID {offlineSubscriptionId}");
        }
        
        // chat message
        Dictionary<string, string> messageCondition = new()
        { 
            { "broadcaster_user_id", channelId }, 
            { "user_id", TwitchConfig.Service().Name } 
        };
        
        string? channelChatMessageSubscriptionId = await _twitchApiService.CreateEventSubSubscription(
            "channel.chat.message", "1",
            messageCondition, callbackUrl, appAccessToken);
        
        if (channelChatMessageSubscriptionId != null) 
        {
            _channelSubscriptionIds[$"chat_message_{channelId}"] = channelChatMessageSubscriptionId;
            // _logger.LogInformation($"Subscribed to channel.chat.message for channel {channelId} with subscription ID {channelChatMessageSubscriptionId}");
        }
    }
    
    private async Task UnsubscribeFromAllChannels()
    {
        _logger.LogInformation("Unsubscribing from events for all channels...");
        
        await _twitchApiService.DeleteAllEventSubSubscriptions(TwitchConfig.Service().AccessToken);

        _channelSubscriptionIds.Clear();
    }
}
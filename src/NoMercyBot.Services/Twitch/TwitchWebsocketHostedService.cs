using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Database.Models.ChatMessage;
using NoMercyBot.Services.Other;
using NoMercyBot.Services.Widgets;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Stream;
using Stream = NoMercyBot.Database.Models.Stream;
using UserUpdateArgs = TwitchLib.EventSub.Websockets.Core.EventArgs.User.UserUpdateArgs;

namespace NoMercyBot.Services.Twitch;

public class TwitchWebsocketHostedService : IHostedService
{
    private readonly IServiceScope _scope;
    private readonly AppDbContext _dbContext;
    private readonly EventSubWebsocketClient _eventSubWebsocketClient;
    private readonly ILogger<TwitchWebsocketHostedService> _logger;
    private CancellationTokenSource _cts = new();
    private readonly TwitchAPI _twitchApi = new();
    private readonly TwitchApiService _twitchApiService;
    private readonly TwitchEventSubService _twitchEventSubService;
    private readonly TwitchMessageDecorator _twitchMessageDecorator;
    private readonly IWidgetEventService _widgetEventService;
    private readonly TwitchChatService _twitchChatService;
    private readonly TwitchCommandService _twitchCommandService;
    private readonly TtsService _ttsService;
    private bool _isConnected;
    private Stream? _currentStream;

    public TwitchWebsocketHostedService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TwitchWebsocketHostedService> logger,
        EventSubWebsocketClient eventSubWebsocketClient,
        TwitchApiService twitchApiService,
        TwitchEventSubService twitchEventSubService, 
        TwitchMessageDecorator twitchMessageDecorator,
        TtsService ttsService,
        TwitchCommandService twitchCommandService,
        TwitchChatService twitchTwitchChatServiceService,
        IWidgetEventService widgetEventService)
    {
        _scope = serviceScopeFactory.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _logger = logger;
        _eventSubWebsocketClient = eventSubWebsocketClient;
        _twitchApiService = twitchApiService;
        _twitchEventSubService = twitchEventSubService;
        _twitchMessageDecorator = twitchMessageDecorator;
        _widgetEventService = widgetEventService;
        _twitchCommandService = twitchCommandService;
        _twitchChatService = twitchTwitchChatServiceService;
        _ttsService = ttsService;
        
        // Subscribe to the event
        twitchEventSubService.OnEventSubscriptionChanged += HandleEventSubscriptionChange;
        
        _currentStream = _dbContext.Streams
            .FirstOrDefault(stream => stream.UpdatedAt == stream.CreatedAt);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TwitchWebsocketHostedService starting.");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Basic connection events
        _eventSubWebsocketClient.WebsocketConnected += OnWebsocketConnected;
        _eventSubWebsocketClient.WebsocketDisconnected += OnWebsocketDisconnected;
        _eventSubWebsocketClient.WebsocketReconnected += OnWebsocketReconnected;
        _eventSubWebsocketClient.ErrorOccurred += OnError;

        // User events
        _eventSubWebsocketClient.UserUpdate += OnUserUpdate;

        // Channel events
        _eventSubWebsocketClient.ChannelUpdate += OnChannelUpdate;
        _eventSubWebsocketClient.ChannelFollow += OnChannelFollow;
        _eventSubWebsocketClient.ChannelSubscribe += OnChannelSubscribe;
        _eventSubWebsocketClient.ChannelSubscriptionGift += OnChannelSubscriptionGift;
        _eventSubWebsocketClient.ChannelSubscriptionMessage += OnChannelSubscriptionMessage;
        _eventSubWebsocketClient.ChannelCheer += OnChannelCheer;
        _eventSubWebsocketClient.ChannelRaid += OnChannelRaid;
        _eventSubWebsocketClient.ChannelBan += OnChannelBan;
        _eventSubWebsocketClient.ChannelUnban += OnChannelUnban;
        _eventSubWebsocketClient.ChannelModeratorAdd += OnChannelModeratorAdd;
        _eventSubWebsocketClient.ChannelModeratorRemove += OnChannelModeratorRemove;

        // Channel chat events
        _eventSubWebsocketClient.ChannelChatMessage += OnChannelChatMessage;
        _eventSubWebsocketClient.ChannelChatClear += OnChannelChatClear;
        _eventSubWebsocketClient.ChannelChatClearUserMessages += OnChannelChatClearUserMessages;
        _eventSubWebsocketClient.ChannelChatMessageDelete += OnChannelChatMessageDelete;
        _eventSubWebsocketClient.ChannelChatNotification += OnChannelChatNotification;

        // Stream events
        _eventSubWebsocketClient.StreamOnline += OnStreamOnline;
        _eventSubWebsocketClient.StreamOffline += OnStreamOffline;

        // Channel points events
        _eventSubWebsocketClient.ChannelPointsCustomRewardAdd += OnChannelPointsCustomRewardAdd;
        _eventSubWebsocketClient.ChannelPointsCustomRewardUpdate += OnChannelPointsCustomRewardUpdate;
        _eventSubWebsocketClient.ChannelPointsCustomRewardRemove += OnChannelPointsCustomRewardRemove;
        _eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionAdd += OnChannelPointsCustomRewardRedemptionAdd;
        _eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionUpdate += OnChannelPointsCustomRewardRedemptionUpdate;

        // Poll events
        _eventSubWebsocketClient.ChannelPollBegin += OnChannelPollBegin;
        _eventSubWebsocketClient.ChannelPollProgress += OnChannelPollProgress;
        _eventSubWebsocketClient.ChannelPollEnd += OnChannelPollEnd;

        // Prediction events
        _eventSubWebsocketClient.ChannelPredictionBegin += OnChannelPredictionBegin;
        _eventSubWebsocketClient.ChannelPredictionProgress += OnChannelPredictionProgress;
        _eventSubWebsocketClient.ChannelPredictionLock += OnChannelPredictionLock;
        _eventSubWebsocketClient.ChannelPredictionEnd += OnChannelPredictionEnd;

        // Hype Train events
        _eventSubWebsocketClient.ChannelHypeTrainBeginV2 += OnHypeTrainBegin;
        _eventSubWebsocketClient.ChannelHypeTrainProgressV2 += OnHypeTrainProgress;
        _eventSubWebsocketClient.ChannelHypeTrainEndV2 += OnHypeTrainEnd;

        // AutoMod events
        // _eventSubWebsocketClient.AutomodMessageHold += OnAutomodMessageHold;
        // _eventSubWebsocketClient.AutomodMessageUpdate += OnAutomodMessageUpdate;
        // _eventSubWebsocketClient.AutomodTermsUpdate += OnAutomodTermsUpdate;

        // Other events
        _eventSubWebsocketClient.ChannelShieldModeBegin += OnChannelShieldModeBegin;
        _eventSubWebsocketClient.ChannelShieldModeEnd += OnChannelShieldModeEnd;
        _eventSubWebsocketClient.ChannelShoutoutCreate += OnShoutoutCreate;
        _eventSubWebsocketClient.ChannelShoutoutReceive += OnShoutoutReceived;

        // Set up TwitchAPI credentials
        _twitchApi.Settings.ClientId = TwitchConfig.Service().ClientId;
        _twitchApi.Settings.Secret = TwitchConfig.Service().ClientSecret;
        _twitchApi.Settings.AccessToken = TwitchConfig.Service().AccessToken;

        // Connect to EventSub WebSocket
        await _eventSubWebsocketClient.ConnectAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TwitchWebsocketHostedService stopping.");

        // Unsubscribe from event changes
        _twitchEventSubService.OnEventSubscriptionChanged -= HandleEventSubscriptionChange;

        try
        {
            // Cancel the internal CTS first to stop any ongoing operations
            await _cts.CancelAsync();
            
            // Try to disconnect with a timeout to prevent hanging
            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5)); // 5-second timeout for disconnect
            
            await _eventSubWebsocketClient.DisconnectAsync();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("TwitchWebsocketHostedService shutdown was cancelled or timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during TwitchWebsocketHostedService shutdown");
        }
        
        _logger.LogInformation("TwitchWebsocketHostedService stopped.");
    }

    private async Task OnWebsocketConnected(object sender, WebsocketConnectedArgs e)
    {
        _logger.LogInformation("Twitch EventSub WebSocket connected. Session ID: {SessionId}",
            _eventSubWebsocketClient.SessionId);
        _isConnected = true;

        if (!e.IsRequestedReconnect)
        {
            // Get broadcaster ID from configuration
            string? accessToken = TwitchConfig.Service().AccessToken;
            string broadcasterId = TwitchConfig.Service().UserId;

            if (string.IsNullOrEmpty(broadcasterId) || string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("Cannot subscribe to events: Missing broadcaster ID or access token");
                return;
            }

            try
            {
                // Get all enabled Twitch event subscriptions from the database
                List<EventSubscription> enabledSubscriptions = await _dbContext.EventSubscriptions
                    .Where(s => s.Provider == "twitch" && s.Enabled)
                    .ToListAsync(_cts.Token);

                if (enabledSubscriptions.Count == 0)
                {
                    _logger.LogInformation("No enabled Twitch event subscriptions found");
                    return;
                }

                _logger.LogInformation("Subscribing to {Count} enabled Twitch events via websocket",
                    enabledSubscriptions.Count);

                // Subscribe to all enabled events
                foreach (EventSubscription subscription in enabledSubscriptions)
                {
                    try
                    {
                        // Different events have different condition requirements
                        // Create condition based on event type
                        Dictionary<string, string> condition =
                            CreateConditionForEvent(subscription.EventType, broadcasterId);

                        // Create subscription using websocket transport
                        await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync(
                            subscription.EventType,
                            subscription.Version,
                            condition,
                            EventSubTransportMethod.Websocket,
                            _eventSubWebsocketClient.SessionId,
                            accessToken: accessToken);

                        // Update the SessionId in the database
                        subscription.SessionId = _eventSubWebsocketClient.SessionId;
                        subscription.UpdatedAt = DateTime.UtcNow;
                        _dbContext.EventSubscriptions.Update(subscription);

                        _logger.LogInformation(
                            "Successfully subscribed to {EventType} (version {Version}) via websocket",
                            subscription.EventType, subscription.Version ?? "1");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to subscribe to event {EventType}: {Message}",
                            subscription.EventType, ex.Message);
                    }
                }

                // Save all subscription changes at once
                await _dbContext.SaveChangesAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to Twitch events via websocket");
            }
        }
    }

    private async Task OnWebsocketDisconnected(object sender, EventArgs e)
    {
        _logger.LogError($"Websocket {_eventSubWebsocketClient.SessionId} disconnected!");

        // Don't do this in production. You should implement a better reconnect strategy
        while (!await _eventSubWebsocketClient.ReconnectAsync())
        {
            _logger.LogError("Websocket reconnect failed!");
            await Task.Delay(1000);
        }
    }

    private async Task OnWebsocketReconnected(object sender, EventArgs e)
    {
        _logger.LogWarning($"Websocket {_eventSubWebsocketClient.SessionId} reconnected");
    }

    private async Task OnError(object sender, ErrorOccuredArgs args)
    {
        _logger.LogError($"Websocket {_eventSubWebsocketClient.SessionId} - Error occurred!");
        
        await SaveChannelEvent(
            Guid.NewGuid().ToString(),
            "websocket.error",
            args.Exception
        );
        
        await Task.CompletedTask;
    }

    #region User Events

    private async Task OnUserUpdate(object sender, UserUpdateArgs args)
    {
        _logger.LogInformation("User updated: {User}", args.Notification.Payload.Event);
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "user.update",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.UserId
        );
        
        User user = await _twitchApiService.FetchUser(id: args.Notification.Payload.Event.UserId);
        
        // Update user information in the database
        await _dbContext.Users
            .Upsert(user)
            .On(u => u.Id)
            .WhenMatched((u, n) => new()
            {
                DisplayName = n.DisplayName,
                ProfileImageUrl = n.ProfileImageUrl,
                OfflineImageUrl = n.OfflineImageUrl,
                Description = n.Description,
                BroadcasterType = n.BroadcasterType,
                UpdatedAt = DateTime.UtcNow
            })
            .RunAsync();
        
        _logger.LogInformation("Updated user info for {User}", args.Notification.Payload.Event.UserLogin);
        
        await Task.CompletedTask;
    }

    #endregion

    #region Channel Events

    private async Task OnChannelUpdate(object sender, ChannelUpdateArgs args)
    {
        _logger.LogInformation("Channel updated: {Channel}", args.Notification.Payload.Event);
        
        await _dbContext.ChannelInfo
            .Where(c => c.Id == args.Notification.Payload.Event.BroadcasterUserId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(c => c.Title, args.Notification.Payload.Event.Title)
                .SetProperty(c => c.Language, args.Notification.Payload.Event.Language)
                .SetProperty(c => c.GameId, args.Notification.Payload.Event.CategoryId)
                .SetProperty(c => c.GameName, args.Notification.Payload.Event.CategoryName)
                .SetProperty(c => c.ContentLabels, args.Notification.Payload.Event.ContentClassificationLabels.ToList())
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow));
        
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.update",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );
        
        _logger.LogInformation("Updated channel info for {Channel}",
            args.Notification.Payload.Event.BroadcasterUserLogin);
    }

    private async Task OnChannelFollow(object sender, ChannelFollowArgs args)
    {
        _logger.LogInformation("Follow: {User} followed {Channel}",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.BroadcasterUserLogin);
        
        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            $"Thanks for following, @{args.Notification.Payload.Event.UserName}! Welcome to the channel!");
            
        await _widgetEventService.PublishEventAsync("channel.follow", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
        });
        
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.follow",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelSubscribe(object sender, ChannelSubscribeArgs args)
    {
        _logger.LogInformation("Subscribe: {User} subscribed to {Channel} at tier {Tier}",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.BroadcasterUserLogin,
            args.Notification.Payload.Event.Tier);
        
        await _widgetEventService.PublishEventAsync("channel.subscribe", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
            { "tier", args.Notification.Payload.Event.Tier },
            { "isGift", args.Notification.Payload.Event.IsGift.ToString() },
        });
        
        if (args.Notification.Payload.Event.IsGift)
        {
            await _twitchChatService.SendMessageAsBot(
                args.Notification.Payload.Event.BroadcasterUserLogin,
                $"@{args.Notification.Payload.Event.UserName} been gifted a tier {args.Notification.Payload.Event.Tier} subscription! Thank you!");
        }
        else
        {
            await _twitchChatService.SendMessageAsBot(
                args.Notification.Payload.Event.BroadcasterUserLogin,
                $"@{args.Notification.Payload.Event.UserName} just subscribed at tier {args.Notification.Payload.Event.Tier}! Thank you!");
        }
        
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.subscribe",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );
        
        await Task.CompletedTask;
    }

    private async Task OnChannelSubscriptionGift(object sender, ChannelSubscriptionGiftArgs args)
    {
        _logger.LogInformation("Subscription gift: {User} gifted {Count} subs to {Channel}",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.Total,
            args.Notification.Payload.Event.BroadcasterUserLogin);
        
        await _widgetEventService.PublishEventAsync("channel.subscription.gift", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
            { "count", args.Notification.Payload.Event.Total.ToString() },
            { "tier", args.Notification.Payload.Event.Tier },
            { "cumulativeTotal", args.Notification.Payload.Event.CumulativeTotal?.ToString() },
            { "isAnonymous", args.Notification.Payload.Event.IsAnonymous.ToString() }
        });
        
        if (args.Notification.Payload.Event.IsAnonymous)
        {
            await _twitchChatService.SendMessageAsBot(
                args.Notification.Payload.Event.BroadcasterUserLogin,
                $"A generous user just gifted {args.Notification.Payload.Event.Total} subs! Thank you!");
        }
        else
        {
            await _twitchChatService.SendMessageAsBot(
                args.Notification.Payload.Event.BroadcasterUserLogin,
                $"@{args.Notification.Payload.Event.UserName} just gifted {args.Notification.Payload.Event.Total} subs! Thank you!");
        }
        
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.subscription.gift",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );
        
        await Task.CompletedTask;
    }

    private async Task OnChannelSubscriptionMessage(object sender, ChannelSubscriptionMessageArgs args)
    {
        _logger.LogInformation("Resubscribe message: {User} resubscribed to {Channel} for {Months} months",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.BroadcasterUserLogin,
            args.Notification.Payload.Event.CumulativeMonths);

        string message = args.Notification.Payload.Event.Message.Text;

        await _widgetEventService.PublishEventAsync("channel.subscription.message", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
            { "months", args.Notification.Payload.Event.CumulativeMonths.ToString() },
            { "tier", args.Notification.Payload.Event.Tier },
            { "streak" , args.Notification.Payload.Event.StreakMonths?.ToString() },
            { "message", message }
        });
        
        if (args.Notification.Payload.Event.StreakMonths > 0)
        {
            await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            $"@{args.Notification.Payload.Event.UserName} just resubscribed for {args.Notification.Payload.Event.CumulativeMonths} months with a {args.Notification.Payload.Event.StreakMonths}-month streak! You're awesome!");
        }
        else
        {
            await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            $"@{args.Notification.Payload.Event.UserName} just resubscribed for {args.Notification.Payload.Event.CumulativeMonths} months! You're awesome!");
        }
        
        await _ttsService.SendTts(message, args.Notification.Payload.Event.UserId, _cts.Token);
        
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.subscription.message",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelCheer(object sender, ChannelCheerArgs args)
    {
        _logger.LogInformation("Cheer: {User} cheered {Bits} bits in {Channel}",
            args.Notification.Payload.Event.IsAnonymous ? "Anonymous" : args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.Bits,
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await _widgetEventService.PublishEventAsync("channel.cheer", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
            { "bits", args.Notification.Payload.Event.Bits.ToString() },
            { "isAnonymous", args.Notification.Payload.Event.IsAnonymous.ToString() },
            { "message", args.Notification.Payload.Event.Message }
        });
        
        if (!args.Notification.Payload.Event.IsAnonymous)
        {
            await _twitchChatService.SendMessageAsBot(
                args.Notification.Payload.Event.BroadcasterUserLogin,
                $"@{args.Notification.Payload.Event.UserName} just cheered {args.Notification.Payload.Event.Bits} bits! Thank you!");
        }
        else
        {
            await _twitchChatService.SendMessageAsBot(
                args.Notification.Payload.Event.BroadcasterUserLogin,
                $"An anonymous user just cheered {args.Notification.Payload.Event.Bits} bits! Thank you!");
        }
        
        // Send TTS if the message is not empty and the user is not anonymous
        if (!string.IsNullOrEmpty(args.Notification.Payload.Event.Message) && !args.Notification.Payload.Event.IsAnonymous)
        {
            await _ttsService.SendTts(args.Notification.Payload.Event.Message, args.Notification.Payload.Event.UserId, _cts.Token);
        }

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.cheer",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );
        
        await Task.CompletedTask;
    }

    private async Task OnChannelRaid(object sender, ChannelRaidArgs args)
    {
        _logger.LogInformation("Raid: {FromChannel} raided {ToChannel} with {Viewers} viewers",
            args.Notification.Payload.Event.FromBroadcasterUserLogin,
            args.Notification.Payload.Event.ToBroadcasterUserLogin,
            args.Notification.Payload.Event.Viewers);

        await _widgetEventService.PublishEventAsync("channel.raid", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.FromBroadcasterUserName },
            { "viewers", args.Notification.Payload.Event.Viewers.ToString() }
        });
        
        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.ToBroadcasterUserLogin,
            $"@{args.Notification.Payload.Event.FromBroadcasterUserName} just raided with {args.Notification.Payload.Event.Viewers} viewers! Welcome raiders!");
        
        // If the raid is from a different channel, send a message to the raider's channel
        if (args.Notification.Payload.Event.FromBroadcasterUserLogin != args.Notification.Payload.Event.ToBroadcasterUserLogin)
        {
            await _twitchChatService.SendMessageAsBot(
                args.Notification.Payload.Event.FromBroadcasterUserLogin,
                $"Thank you for the raid, @{args.Notification.Payload.Event.ToBroadcasterUserName}! We appreciate your support!");
        } 

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.raid",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.ToBroadcasterUserId,
            args.Notification.Payload.Event.FromBroadcasterUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelBan(object sender, ChannelBanArgs args)
    {
        _logger.LogInformation("Ban: {User} was banned from {Channel} by {Moderator}. Reason: {Reason}",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.BroadcasterUserLogin,
            args.Notification.Payload.Event.ModeratorUserLogin,
            args.Notification.Payload.Event.Reason);

        await _widgetEventService.PublishEventAsync("channel.ban", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
            { "moderator", args.Notification.Payload.Event.ModeratorUserName },
            { "reason", args.Notification.Payload.Event.Reason }
        });
        
        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            $"@{args.Notification.Payload.Event.UserName} has been banned from the channel. Reason: {args.Notification.Payload.Event.Reason}");
        
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.ban",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelUnban(object sender, ChannelUnbanArgs args)
    {
        _logger.LogInformation("Unban: {User} was unbanned from {Channel} by {Moderator}",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.BroadcasterUserLogin,
            args.Notification.Payload.Event.ModeratorUserLogin);

        await _widgetEventService.PublishEventAsync("channel.unban", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
            { "moderator", args.Notification.Payload.Event.ModeratorUserName }
        });
        
        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            $"@{args.Notification.Payload.Event.UserName} has been unbanned from the channel.");
        
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.unban",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelModeratorAdd(object sender, ChannelModeratorArgs args)
    {
        _logger.LogInformation("Mod add: {User} was modded in {Channel}",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await _widgetEventService.PublishEventAsync("channel.moderator.add", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
            { "broadcaster", args.Notification.Payload.Event.BroadcasterUserName }
        });
        
        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            $"@{args.Notification.Payload.Event.UserName} has been added as a moderator in the channel.");
        
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.moderator.add",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelModeratorRemove(object sender, ChannelModeratorArgs args)
    {
        _logger.LogInformation("Mod remove: {User} was unmodded in {Channel}",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await _widgetEventService.PublishEventAsync("channel.moderator.remove", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
            { "broadcaster", args.Notification.Payload.Event.BroadcasterUserName }
        });
        
        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            $"@{args.Notification.Payload.Event.UserName} has been removed as a moderator in the channel.");
        
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.moderator.remove",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );

        await Task.CompletedTask;
    }

    #endregion

    #region Chat Events

    private async Task OnChannelChatMessage(object sender, ChannelChatMessageArgs args)
    {
        _logger.LogInformation("Chat message: {User} in {Channel}: {Message}",
            args.Notification.Payload.Event.ChatterUserLogin,
            args.Notification.Payload.Event.BroadcasterUserLogin,
            args.Notification.Payload.Event.Message.Text);

        try
        {
            User? user = _dbContext.Users.FirstOrDefault(u => u.Id == args.Notification.Payload.Event.ChatterUserId);
            User? broadcaster = _dbContext.Users.FirstOrDefault(u => u.Id == args.Notification.Payload.Event.BroadcasterUserId);

            user ??= await _twitchApiService.FetchUser(id: args.Notification.Payload.Event.ChatterUserId);

            ChatMessage chatMessage = new(args.Notification, _currentStream, user, broadcaster);

            await _twitchMessageDecorator.DecorateMessage(chatMessage);

            if (chatMessage.IsCommand)
            {
                await _twitchCommandService.ExecuteCommand(chatMessage);
            }
            else {
                await _widgetEventService.PublishEventAsync("twitch.chat.message", chatMessage);
            }

            await Task.Delay(1000, _cts.Token);
            await _ttsService.SendTts(chatMessage.Fragments, chatMessage.UserId, _cts.Token);
            
            await _dbContext.ChatMessages
                .Upsert(chatMessage)
                .RunAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to save chat message from {User} in {Ex}",
                args.Notification.Payload.Event.ChatterUserLogin,
                e.Message);
            throw;
        }

        await Task.CompletedTask;
    }

    private async Task OnChannelChatClear(object sender, ChannelChatClearArgs args)
    {
        _logger.LogInformation("Chat clear: Chat was cleared in {Channel}",
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await _dbContext.ChatMessages
            .Where(c => _currentStream != null && c.StreamId == _currentStream.Id)
            .ExecuteUpdateAsync(u => u
                .SetProperty(c => c.DeletedAt, DateTime.UtcNow)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow));

        await _widgetEventService.PublishEventAsync("channel.chat.clear", new());

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.chat.clear",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelChatClearUserMessages(object sender, ChannelChatClearUserMessagesArgs args)
    {
        _logger.LogInformation("User messages cleared: {User}'s messages were cleared in {Channel}",
            args.Notification.Payload.Event.TargetUserLogin,
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await _dbContext.ChatMessages.Where(c => _currentStream != null && c.StreamId == _currentStream.Id
                        && c.UserId == args.Notification.Payload.Event.TargetUserId)
            .ExecuteUpdateAsync(
                u => u
                    .SetProperty(c => c.DeletedAt, DateTime.UtcNow)
                    .SetProperty(c => c.UpdatedAt, DateTime.UtcNow));
        
        _logger.LogInformation("Marked messages as deleted for user {User} in {Channel}",
            args.Notification.Payload.Event.TargetUserLogin,
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.chat.clear.user.messages",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.TargetUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelChatMessageDelete(object sender, ChannelChatMessageDeleteArgs args)
    {
        _logger.LogInformation("Message deleted: A message from {User} was deleted in {Channel}",
            args.Notification.Payload.Event.TargetUserLogin,
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await _dbContext.ChatMessages
            .Where(c => c.Id == args.Notification.Payload.Event.MessageId)
            .ExecuteUpdateAsync(
            u => u
                .SetProperty(c => c.DeletedAt, DateTime.UtcNow)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow));
            
        _logger.LogInformation("Marked message as deleted: {MessageId} in {Channel}",
            args.Notification.Payload.Event.MessageId, 
            args.Notification.Payload.Event.BroadcasterUserLogin);
        
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.chat.message.delete",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.TargetUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelChatNotification(object sender, ChannelChatNotificationArgs args)
    {
        _logger.LogInformation("Chat notification in {Channel}: {Message}",
            args.Notification.Payload.Event.BroadcasterUserLogin,
            args.Notification.Payload.Event.Message.Text);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.chat.notification",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await Task.CompletedTask;
    }

    #endregion

    #region Stream Events

    private async Task OnStreamOnline(object sender, StreamOnlineArgs args)
    {
        _logger.LogInformation("Stream online: {Channel}", args.Notification.Payload.Event.BroadcasterUserLogin);

        try
        {

            ChannelInfo? channelInfo = await _dbContext.ChannelInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == args.Notification.Payload.Event.BroadcasterUserId, _cts.Token);

            if (channelInfo != null)
            {
                Stream stream = new()
                {
                    Id = args.Notification.Payload.Event.Id,
                    ChannelId = args.Notification.Payload.Event.BroadcasterUserId,
                    Title = channelInfo.Title,
                    GameId = channelInfo.GameId,
                    GameName = channelInfo.GameName,
                    Language = channelInfo.Language,
                    Delay = channelInfo.Delay,
                    Tags = channelInfo.Tags,
                    ContentLabels = channelInfo.ContentLabels,
                    IsBrandedContent = channelInfo.IsBrandedContent
                };

                _currentStream = stream;

                await _dbContext.Streams.Upsert(stream)
                    .On(p => p.Id)
                    .WhenMatched((db, entity) => new()
                    {
                        Title = entity.Title,
                        GameId = entity.GameId,
                        GameName = entity.GameName,
                        Language = entity.Language,
                        Delay = entity.Delay,
                        Tags = entity.Tags,
                        ContentLabels = entity.ContentLabels,
                        IsBrandedContent = entity.IsBrandedContent,
                    })
                    .RunAsync();

                _logger.LogInformation("Created new stream entry for {Channel} with ID {StreamId}",
                    args.Notification.Payload.Event.BroadcasterUserLogin, stream.Id);

                await _dbContext.ChannelInfo
                    .Where(c => c.Id == channelInfo.Id)
                    .ExecuteUpdateAsync(u => u
                        .SetProperty(c => c.IsLive, true)
                        .SetProperty(c => c.UpdatedAt, DateTime.UtcNow));

                _logger.LogInformation("Updated stream status to online for {Channel}",
                    args.Notification.Payload.Event.BroadcasterUserLogin);
            }
        } catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle stream online event for {Channel}: {Message}",
                args.Notification.Payload.Event.BroadcasterUserLogin, ex.Message);
        }
        
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "stream.online",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

    }

    private async Task OnStreamOffline(object sender, StreamOfflineArgs args)
    {
        _logger.LogInformation("Stream offline: {Channel}", args.Notification.Payload.Event.BroadcasterUserLogin);
        
        _currentStream = null;

        await _dbContext.ChannelInfo
            .Where(c => c.Id == args.Notification.Payload.Event.BroadcasterUserId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(c => c.IsLive, true)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow));
        
        await _dbContext.Streams
            .OrderByDescending(s => s.CreatedAt)
            .Where(s => s.ChannelId == args.Notification.Payload.Event.BroadcasterUserId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(s => s.UpdatedAt, DateTime.UtcNow));
        
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "stream.offline",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        _logger.LogInformation("Updated stream status to offline for {Channel}",
            args.Notification.Payload.Event.BroadcasterUserLogin);
    }

    #endregion

    #region Channel Points Events

    private async Task OnChannelPointsCustomRewardAdd(object sender, ChannelPointsCustomRewardArgs args)
    {
        _logger.LogInformation("Custom reward added: {Title} in {Channel}",
            args.Notification.Payload.Event.Title,
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.points.custom.reward.add",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelPointsCustomRewardUpdate(object sender, ChannelPointsCustomRewardArgs args)
    {
        _logger.LogInformation("Custom reward updated: {Title} in {Channel}",
            args.Notification.Payload.Event.Title,
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.points.custom.reward.update",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelPointsCustomRewardRemove(object sender, ChannelPointsCustomRewardArgs args)
    {
        _logger.LogInformation("Custom reward removed: {Title} in {Channel}",
            args.Notification.Payload.Event.Title,
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.points.custom.reward.remove",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelPointsCustomRewardRedemptionAdd(object sender,
        ChannelPointsCustomRewardRedemptionArgs args)
    {
        _logger.LogInformation("Reward redeemed: {User} redeemed {Title} in {Channel}",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.Reward.Title,
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.points.custom.reward.redemption.add",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelPointsCustomRewardRedemptionUpdate(object sender,
        ChannelPointsCustomRewardRedemptionArgs args)
    {
        _logger.LogInformation("Reward redemption updated: {User}'s redemption of {Title} in {Channel} was {Status}",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.Reward.Title,
            args.Notification.Payload.Event.BroadcasterUserLogin,
            args.Notification.Payload.Event.Status);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.points.custom.reward.redemption.update",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );

        await Task.CompletedTask;
    }

    #endregion

    #region Poll Events

    private async Task OnChannelPollBegin(object sender, ChannelPollBeginArgs args)
    {
        _logger.LogInformation("Poll started: \"{Title}\" in {Channel}",
            args.Notification.Payload.Event.Title,
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.poll.begin",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelPollProgress(object sender, ChannelPollProgressArgs args)
    {
        _logger.LogInformation("Poll progress: \"{Title}\" in {Channel}",
            args.Notification.Payload.Event.Title,
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.poll.progress",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelPollEnd(object sender, ChannelPollEndArgs args)
    {
        _logger.LogInformation("Poll ended: \"{Title}\" in {Channel}. Status: {Status}",
            args.Notification.Payload.Event.Title,
            args.Notification.Payload.Event.BroadcasterUserLogin,
            args.Notification.Payload.Event.Status);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.poll.end",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await Task.CompletedTask;
    }

    #endregion

    #region Prediction Events

    private async Task OnChannelPredictionBegin(object sender, ChannelPredictionBeginArgs args)
    {
        _logger.LogInformation("Prediction started: \"{Title}\" in {Channel}",
            args.Notification.Payload.Event.Title,
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.prediction.begin",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelPredictionProgress(object sender, ChannelPredictionProgressArgs args)
    {
        _logger.LogInformation("Prediction progress: \"{Title}\" in {Channel}",
            args.Notification.Payload.Event.Title,
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.prediction.progress",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelPredictionLock(object sender, ChannelPredictionLockArgs args)
    {
        _logger.LogInformation("Prediction locked: \"{Title}\" in {Channel}",
            args.Notification.Payload.Event.Title,
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.prediction.lock",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelPredictionEnd(object sender, ChannelPredictionEndArgs args)
    {
        _logger.LogInformation("Prediction ended: \"{Title}\" in {Channel}. Status: {Status}",
            args.Notification.Payload.Event.Title,
            args.Notification.Payload.Event.BroadcasterUserLogin,
            args.Notification.Payload.Event.Status);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.prediction.end",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await Task.CompletedTask;
    }

    #endregion

    #region Hype Train Events

    private async Task OnHypeTrainBegin(object sender, ChannelHypeTrainBeginV2Args args)
    {
        _logger.LogInformation("Hype Train started in {Channel}",
            args.Notification.Payload.Event.BroadcasterUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.hype.train.begin",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnHypeTrainProgress(object sender, ChannelHypeTrainProgressV2Args args)
    {
        _logger.LogInformation("Hype Train progress in {Channel}: Level {Level}, {Points}/{Goal} points",
            args.Notification.Payload.Event.BroadcasterUserLogin,
            args.Notification.Payload.Event.Level,
            args.Notification.Payload.Event.Progress,
            args.Notification.Payload.Event.Goal);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.hype.train.progress",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnHypeTrainEnd(object sender, ChannelHypeTrainEndV2Args args)
    {
        _logger.LogInformation("Hype Train ended in {Channel}. Reached Level {Level}",
            args.Notification.Payload.Event.BroadcasterUserLogin,
            args.Notification.Payload.Event.Level);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.hype.train.end",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await Task.CompletedTask;
    }

    #endregion

    #region AutoMod Events

    // private async Task OnAutomodMessageHold(object sender, object args)
    // {
    //     _logger.LogInformation("AutoMod held message from {User} in {Channel}",
    //         args.Notification.Payload.Event.MessageSenderLogin,
    //         args.Notification.Payload.Event.BroadcasterUserLogin);
    //
    //     await Task.CompletedTask;
    // }

    // private async Task OnAutomodMessageUpdate(object sender, object args)
    // {
    //     _logger.LogInformation("AutoMod message status updated in {Channel}. Status: {Status}",
    //         args.Notification.Payload.Event.BroadcasterUserLogin,
    //         args.Notification.Payload.Event.Status);
    //
    //     await Task.CompletedTask;
    // }
    //
    // private async Task OnAutomodTermsUpdate(object sender, AutomodTermsUpdateArgs args)
    // {
    //     _logger.LogInformation("AutoMod terms updated in {Channel} by {Moderator}",
    //         args.Notification.Payload.Event.BroadcasterUserLogin,
    //         args.Notification.Payload.Event.ModeratorUserLogin);
    //
    //     // Log the actual terms data
    //     _logger.LogInformation("AutoMod terms update payload: {Payload}",
    //         JsonConvert.SerializeObject(args.Notification.Payload.Event));
    //
    //     await Task.CompletedTask;
    // }

    #endregion

    #region Other Events

    private async Task OnChannelShieldModeBegin(object sender, ChannelShieldModeBeginArgs args)
    {
        _logger.LogInformation("Shield mode activated in {Channel} by {Moderator}",
            args.Notification.Payload.Event.BroadcasterUserLogin,
            args.Notification.Payload.Event.ModeratorUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.shield.mode.begin",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.ModeratorUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnChannelShieldModeEnd(object sender, ChannelShieldModeEndArgs args)
    {
        _logger.LogInformation("Shield mode deactivated in {Channel} by {Moderator}",
            args.Notification.Payload.Event.BroadcasterUserLogin,
            args.Notification.Payload.Event.ModeratorUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.shield.mode.end",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.ModeratorUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnShoutoutCreate(object sender, ChannelShoutoutCreateArgs args)
    {
        _logger.LogInformation("Shoutout created: {FromChannel} shouted out {ToChannel}",
            args.Notification.Payload.Event.BroadcasterUserLogin,
            args.Notification.Payload.Event.ToBroadcasterUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.shoutout.create",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.ToBroadcasterUserId
        );

        await Task.CompletedTask;
    }

    private async Task OnShoutoutReceived(object sender, ChannelShoutoutReceiveArgs args)
    {
        _logger.LogInformation(
            "Shoutout received: {ToChannel} received shoutout from {FromChannel} with {ViewerCount} viewers",
            args.Notification.Payload.Event.BroadcasterUserLogin,
            args.Notification.Payload.Event.FromBroadcasterUserLogin,
            args.Notification.Payload.Event.ViewerCount);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.shoutout.receive",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.FromBroadcasterUserId
        );

        await Task.CompletedTask;
    }

    #endregion

    // Method to handle event toggling - dynamically subscribe/unsubscribe when events are enabled/disabled
    private async Task HandleEventSubscriptionChange(string eventType, bool enabled)
    {
        _logger.LogInformation("Event subscription changed: {EventType} is now {Status}",
            eventType, enabled ? "enabled" : "disabled");

        // If the websocket is not connected or SessionId is null, we can't subscribe/unsubscribe
        if (!_isConnected || string.IsNullOrEmpty(_eventSubWebsocketClient.SessionId))
        {
            _logger.LogWarning("Cannot modify subscription - WebSocket not connected or SessionId is null");
            return;
        }

        string accessToken = TwitchConfig.Service().AccessToken!;
        string? broadcasterId = _twitchApiService.GetUsers().Result?.FirstOrDefault()?.Id;

        if (string.IsNullOrEmpty(broadcasterId) || string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("Cannot modify subscription: Missing broadcaster ID or access token");
            return;
        }

        try
        {
            if (enabled)
            {
                // Get event subscription details from database
                EventSubscription? subscription = await _dbContext.EventSubscriptions
                    .FirstOrDefaultAsync(s => s.Provider == "twitch" && s.EventType == eventType, _cts.Token);

                if (subscription == null)
                {
                    _logger.LogError("Cannot subscribe to event {EventType} - not found in database", eventType);
                    return;
                }

                // Create condition for this event type
                Dictionary<string, string> condition = CreateConditionForEvent(eventType, broadcasterId);

                await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync(
                    type: eventType,
                    version: subscription.Version,
                    condition: condition,
                    method: EventSubTransportMethod.Websocket,
                    websocketSessionId: _eventSubWebsocketClient.SessionId,
                    clientId: TwitchConfig.Service().ClientId,
                    accessToken: TwitchConfig.Service().AccessToken);

                // Update the SessionId in the database
                subscription.SessionId = _eventSubWebsocketClient.SessionId;
                subscription.UpdatedAt = DateTime.UtcNow;
                _dbContext.EventSubscriptions.Update(subscription);
                await _dbContext.SaveChangesAsync(_cts.Token);

                _logger.LogInformation(
                    "Successfully subscribed to {EventType} (version {Version}) via websocket with session {SessionId}",
                    eventType, subscription.Version ?? "1", _eventSubWebsocketClient.SessionId);
            }
            else
            {
                // For disabling, we need to find and delete the existing subscription
                // First, check if we have the subscription in our database with the current session ID
                EventSubscription? subscription = await _dbContext.EventSubscriptions
                    .FirstOrDefaultAsync(s => s.Provider == "twitch" && s.EventType == eventType, _cts.Token);

                if (subscription != null)
                {
                    // Get the subscription from Twitch API
                    GetEventSubSubscriptionsResponse? twitchSubscriptions =
                        await _twitchApi.Helix.EventSub.GetEventSubSubscriptionsAsync(
                            type: eventType,
                            accessToken: accessToken);

                    if (twitchSubscriptions != null && twitchSubscriptions.Subscriptions.Any())
                    {
                        // Find subscriptions for this event type that use our current websocket session
                        List<EventSubSubscription> activeSubscriptions = twitchSubscriptions.Subscriptions
                            .Where(s => s.Type == eventType &&
                                        s.Transport.Method == "websocket")
                            .ToList();

                        foreach (EventSubSubscription sub in activeSubscriptions)
                        {
                            // Delete the subscription from Twitch
                            await _twitchApi.Helix.EventSub.DeleteEventSubSubscriptionAsync(
                                sub.Id, accessToken);

                            _logger.LogInformation(
                                "Successfully unsubscribed from {EventType} (ID: {Id}, Session: {SessionId})",
                                eventType, sub.Id, _eventSubWebsocketClient.SessionId);
                        }

                        // Clear the SessionId in the database to indicate it's no longer active
                        subscription.SessionId = null;
                        subscription.UpdatedAt = DateTime.UtcNow;
                        _dbContext.EventSubscriptions.Update(subscription);
                        await _dbContext.SaveChangesAsync(_cts.Token);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to {Action} event {EventType}: {Message}",
                enabled ? "subscribe to" : "unsubscribe from", eventType, ex.Message);
        }
    }

    // Helper method to create the right condition for different event types
    private Dictionary<string, string> CreateConditionForEvent(string eventType, string broadcasterId)
    {
        Dictionary<string, string> condition = new();

        // Use the condition information directly from AvailableEventTypes if available
        if (TwitchEventSubService.AvailableEventTypes.TryGetValue(eventType,
                out (string, string, string[] Condition) eventTypeInfo))
        {
            // Apply each required condition parameter
            foreach (string conditionParam in eventTypeInfo.Condition)
            {
                switch (conditionParam)
                {
                    case "broadcaster_user_id":
                        condition["broadcaster_user_id"] = broadcasterId;
                        break;

                    case "to_broadcaster_user_id":
                        condition["to_broadcaster_user_id"] = broadcasterId;
                        break;

                    case "moderator_user_id":
                        // For simplicity, use the broadcaster as the moderator for automod events
                        condition["moderator_user_id"] = broadcasterId;
                        break;

                    case "client_id":
                        condition["client_id"] = TwitchConfig.Service().ClientId!;
                        break;

                    case "user_id":
                        condition["user_id"] = broadcasterId;
                        break;

                    default:
                        _logger.LogWarning("Unknown condition parameter: {ConditionParam} for event type {EventType}",
                            conditionParam, eventType);
                        break;
                }
            }
        }
        else
        {
            // Fallback in case the event type is not found in the dictionary
            _logger.LogWarning(
                "Event type {EventType} not found in AvailableEventTypes, using broadcaster_user_id as default",
                eventType);
            condition["broadcaster_user_id"] = broadcasterId;
        }

        return condition;
    }

    private async Task SaveChannelEvent(string id, string type, object data, string? channelId = null, string? userId = null)
    {
        await _dbContext.ChannelEvents
            .Upsert(new()
            {
                Id = id,
                Type = type,
                Data = data,
                ChannelId = channelId,
                UserId = userId
            })
            .On(p => p.Id)
            .RunAsync();
    }
}


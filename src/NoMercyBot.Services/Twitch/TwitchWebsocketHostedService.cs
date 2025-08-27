using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Services.Other;
using NoMercyBot.Services.Twitch.EventHandlers;
using NoMercyBot.Services.Twitch.EventHandlers.Interfaces;
using NoMercyBot.Services.Widgets;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using Stream = NoMercyBot.Database.Models.Stream;

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
    private readonly List<ITwitchEventHandler> _eventHandlers = [];
    private bool _isConnected;

    // Event handlers
    private readonly UserEventHandler _userEventHandler;
    private readonly ChannelEventHandler _channelEventHandler;
    private readonly MonetizationEventHandler _monetizationEventHandler;
    private readonly ChatEventHandler _chatEventHandler;
    private readonly StreamEventHandler _streamEventHandler;
    private readonly ChannelPointsEventHandler _channelPointsEventHandler;
    private readonly PollEventHandler _pollEventHandler;
    private readonly PredictionEventHandler _predictionEventHandler;
    private readonly HypeTrainEventHandler _hypeTrainEventHandler;
    private readonly OtherEventHandler _otherEventHandler;

    public TwitchWebsocketHostedService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TwitchWebsocketHostedService> logger,
        EventSubWebsocketClient eventSubWebsocketClient,
        TwitchApiService twitchApiService,
        TwitchEventSubService twitchEventSubService,
        TwitchMessageDecorator twitchMessageDecorator,
        TtsService ttsService,
        TwitchCommandService twitchCommandService,
        TwitchRewardService twitchRewardService,
        TwitchChatService twitchChatService,
        IWidgetEventService widgetEventService)
    {
        _scope = serviceScopeFactory.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _logger = logger;
        _eventSubWebsocketClient = eventSubWebsocketClient;
        _twitchApiService = twitchApiService;
        _twitchEventSubService = twitchEventSubService;

        // Initialize event handlers
        _userEventHandler = new(_dbContext, _scope.ServiceProvider.GetRequiredService<ILogger<UserEventHandler>>(), twitchApiService);
        _channelEventHandler = new(_dbContext, _scope.ServiceProvider.GetRequiredService<ILogger<ChannelEventHandler>>(), twitchApiService, ttsService, twitchChatService, widgetEventService, _cts.Token);
        _monetizationEventHandler = new(_dbContext, _scope.ServiceProvider.GetRequiredService<ILogger<MonetizationEventHandler>>(), twitchApiService, twitchChatService, widgetEventService, ttsService, _cts.Token);
        _chatEventHandler = new(_dbContext, _scope.ServiceProvider.GetRequiredService<ILogger<ChatEventHandler>>(), twitchApiService, twitchChatService, twitchCommandService, twitchMessageDecorator, widgetEventService, ttsService, _cts.Token);
        _streamEventHandler = new(_dbContext, _scope.ServiceProvider.GetRequiredService<ILogger<StreamEventHandler>>(), twitchApiService, _cts.Token);
        _channelPointsEventHandler = new(_dbContext, _scope.ServiceProvider.GetRequiredService<ILogger<ChannelPointsEventHandler>>(), twitchApiService, twitchRewardService);
        _pollEventHandler = new(_dbContext, _scope.ServiceProvider.GetRequiredService<ILogger<PollEventHandler>>(), twitchApiService);
        _predictionEventHandler = new(_dbContext, _scope.ServiceProvider.GetRequiredService<ILogger<PredictionEventHandler>>(), twitchApiService);
        _hypeTrainEventHandler = new(_dbContext, _scope.ServiceProvider.GetRequiredService<ILogger<HypeTrainEventHandler>>(), twitchApiService);
        _otherEventHandler = new(_dbContext, _scope.ServiceProvider.GetRequiredService<ILogger<OtherEventHandler>>(), twitchApiService, twitchChatService);

        // Add all handlers to the list
        _eventHandlers.AddRange([
            _userEventHandler,
            _channelEventHandler,
            _monetizationEventHandler,
            _chatEventHandler,
            _streamEventHandler,
            _channelPointsEventHandler,
            _pollEventHandler,
            _predictionEventHandler,
            _hypeTrainEventHandler,
            _otherEventHandler
        ]);

        // Subscribe to the event
        twitchEventSubService.OnEventSubscriptionChanged += HandleEventSubscriptionChange;

        // Initialize current stream reference and pass it to chat handler
        Stream? currentStream = _dbContext.Streams
            .FirstOrDefault(stream => stream.UpdatedAt == stream.CreatedAt);
        _chatEventHandler.SetCurrentStream(currentStream);
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

        // Register all event handlers
        foreach (ITwitchEventHandler handler in _eventHandlers)
        {
            await handler.RegisterEventHandlersAsync(_eventSubWebsocketClient);
        }

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

        // Unregister all event handlers
        foreach (ITwitchEventHandler handler in _eventHandlers)
        {
            await handler.UnregisterEventHandlersAsync(_eventSubWebsocketClient);
        }

        try
        {
            // Cancel the internal CTS first to stop any ongoing operations
            await _cts.CancelAsync();

            // Try to disconnect with a timeout to prevent hanging
            using CancellationTokenSource timeoutCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
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
                await Parallel.ForEachAsync(enabledSubscriptions, async (subscription, _) =>
                {
                    try
                    {
                        // Different events have different condition requirements
                        // Create condition based on event type
                        Dictionary<string, string> condition =
                            CreateConditionForEvent(subscription.EventType, broadcasterId,
                                TwitchConfig.Service().ClientId!);

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
                });

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
                Dictionary<string, string> condition =
                    CreateConditionForEvent(eventType, broadcasterId, TwitchConfig.Service().ClientId!);

                await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync(
                    eventType,
                    subscription.Version,
                    condition,
                    EventSubTransportMethod.Websocket,
                    _eventSubWebsocketClient.SessionId,
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
    private Dictionary<string, string> CreateConditionForEvent(string eventType, string broadcasterId, string clientId,
        string? extensionClientId = null)
    {
        Dictionary<string, string> condition = [];

        // Use the condition information directly from AvailableEventTypes if available
        if (TwitchEventSubService.AvailableEventTypes.TryGetValue(eventType,
                out (string, string, string[] Condition) eventTypeInfo))
        {
            // Apply each required condition parameter
            foreach (string conditionParam in eventTypeInfo.Condition)
                switch (conditionParam)
                {
                    case "broadcaster_user_id":
                        condition["broadcaster_user_id"] = broadcasterId;
                        break;

                    case "to_broadcaster_user_id":
                        condition["to_broadcaster_user_id"] = broadcasterId;
                        break;

                    case "moderator_user_id":
                        condition["moderator_user_id"] = broadcasterId;
                        break;

                    case "client_id":
                        condition["client_id"] = clientId;
                        break;

                    case "user_id":
                        condition["user_id"] = broadcasterId;
                        break;

                    case "extension_client_id":
                        condition["extension_client_id"] = extensionClientId ?? clientId;
                        break;

                    default:
                        _logger.LogWarning("Unknown condition parameter: {ConditionParam} for event type {EventType}",
                            conditionParam, eventType);
                        break;
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
        _ = await _twitchApiService.GetOrFetchUser(userId);

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

    // Property to expose current stream for other handlers
    public Stream? CurrentStream => _streamEventHandler.CurrentStream;

    // Method to update current stream reference across handlers
    private void UpdateCurrentStreamReference(Stream? stream)
    {
        _chatEventHandler.SetCurrentStream(stream);
    }
}
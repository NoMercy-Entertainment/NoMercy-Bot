using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Globals.Extensions;
using NoMercyBot.Services.Other;
using NoMercyBot.Services.Widgets;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace NoMercyBot.Services.Twitch.EventHandlers;

public class MonetizationEventHandler : TwitchEventHandlerBase
{
    private readonly TwitchChatService _twitchChatService;
    private readonly IWidgetEventService _widgetEventService;
    private readonly TtsService _ttsService;
    private readonly CancellationToken _cancellationToken;

    public MonetizationEventHandler(
        AppDbContext dbContext,
        ILogger<MonetizationEventHandler> logger,
        TwitchApiService twitchApiService,
        TwitchChatService twitchChatService,
        IWidgetEventService widgetEventService,
        TtsService ttsService,
        CancellationToken cancellationToken = default)
        : base(dbContext, logger, twitchApiService)
    {
        _twitchChatService = twitchChatService;
        _widgetEventService = widgetEventService;
        _ttsService = ttsService;
        _cancellationToken = cancellationToken;
    }

    public override async Task RegisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient)
    {
        eventSubWebsocketClient.ChannelSubscribe += OnChannelSubscribe;
        eventSubWebsocketClient.ChannelSubscriptionGift += OnChannelSubscriptionGift;
        eventSubWebsocketClient.ChannelSubscriptionMessage += OnChannelSubscriptionMessage;
        eventSubWebsocketClient.ChannelCheer += OnChannelCheer;
        eventSubWebsocketClient.ChannelAdBreakBegin += OnChannelAdBreakBegin;
        await Task.CompletedTask;
    }

    public override async Task UnregisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient)
    {
        eventSubWebsocketClient.ChannelSubscribe -= OnChannelSubscribe;
        eventSubWebsocketClient.ChannelSubscriptionGift -= OnChannelSubscriptionGift;
        eventSubWebsocketClient.ChannelSubscriptionMessage -= OnChannelSubscriptionMessage;
        eventSubWebsocketClient.ChannelCheer -= OnChannelCheer;
        eventSubWebsocketClient.ChannelAdBreakBegin -= OnChannelAdBreakBegin;
        await Task.CompletedTask;
    }

    private async Task OnChannelSubscribe(object sender, ChannelSubscribeArgs args)
    {
        Logger.LogInformation("Subscribe: {User} subscribed at tier {Tier}",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.Tier);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.subscribe",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );

        await _widgetEventService.PublishEventAsync("channel.subscribe", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
            { "tier", args.Notification.Payload.Event.Tier },
            { "isGift", args.Notification.Payload.Event.IsGift.ToString() }
        });

        string message = args.Notification.Payload.Event.IsGift 
            ? $"@{args.Notification.Payload.Event.UserName} been gifted a tier {args.Notification.Payload.Event.Tier} subscription!"
            : $"@{args.Notification.Payload.Event.UserName} just subscribed at tier {args.Notification.Payload.Event.Tier}! Thank you!";

        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            message.ReplaceTierNumbers());
        
        bool widgetSubscriptions = await _widgetEventService.HasWidgetSubscriptionsAsync("channel.chat.message.tts");
        if (widgetSubscriptions)
        {
            await _ttsService.SendCachedTts(
                message.ReplaceTierNumbers(),
                args.Notification.Payload.Event.BroadcasterUserId,
                _cancellationToken);
        }
    }

    private async Task OnChannelSubscriptionGift(object sender, ChannelSubscriptionGiftArgs args)
    {
        Logger.LogInformation("Subscription gift: {User} gifted {Count} subs",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.Total);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.subscription.gift",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );

        await _widgetEventService.PublishEventAsync("channel.subscription.gift", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
            { "count", args.Notification.Payload.Event.Total.ToString() },
            { "tier", args.Notification.Payload.Event.Tier },
            { "cumulativeTotal", args.Notification.Payload.Event.CumulativeTotal?.ToString() },
            { "isAnonymous", args.Notification.Payload.Event.IsAnonymous.ToString() }
        });

        string message = args.Notification.Payload.Event.IsAnonymous 
            ? $"A generous user just gifted {args.Notification.Payload.Event.Total} subs! Thank you!"
            : $"@{args.Notification.Payload.Event.UserName} just gifted {args.Notification.Payload.Event.Total} subs! Thank you!";

        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            message.ReplaceTierNumbers());
        
        bool widgetSubscriptions = await _widgetEventService.HasWidgetSubscriptionsAsync("channel.chat.message.tts");
        if (widgetSubscriptions)
        {
            await _ttsService.SendCachedTts(
                message.ReplaceTierNumbers(),
                args.Notification.Payload.Event.BroadcasterUserId,
                _cancellationToken);
        }
    }

    private async Task OnChannelSubscriptionMessage(object sender, ChannelSubscriptionMessageArgs args)
    {
        Logger.LogInformation("Resubscribe message: {User} resubscribed for {Months} months",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.CumulativeMonths);
        
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.subscription.message",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );
        
        string eventMessage = args.Notification.Payload.Event.Message.Text;

        await _widgetEventService.PublishEventAsync("channel.subscription.message", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
            { "months", args.Notification.Payload.Event.CumulativeMonths.ToString() },
            { "tier", args.Notification.Payload.Event.Tier },
            { "streak", args.Notification.Payload.Event.StreakMonths?.ToString() },
            { "message", eventMessage }
        });

        string chatMessage = args.Notification.Payload.Event.StreakMonths > 0
            ? $"@{args.Notification.Payload.Event.UserName} just resubscribed for {args.Notification.Payload.Event.CumulativeMonths} months with a {args.Notification.Payload.Event.StreakMonths}-month streak! You're awesome!"
            : $"@{args.Notification.Payload.Event.UserName} just resubscribed for {args.Notification.Payload.Event.CumulativeMonths} months! You're awesome!";

        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            chatMessage.ReplaceTierNumbers());

        // Handle TTS for subscription message if widgets are subscribed
        bool widgetSubscriptions = await _widgetEventService.HasWidgetSubscriptionsAsync("channel.chat.message.tts");
        if (widgetSubscriptions && !string.IsNullOrEmpty(eventMessage))
        {
            await _ttsService.SendCachedTts(
                eventMessage.ReplaceTierNumbers(), 
                args.Notification.Payload.Event.BroadcasterUserId, 
                _cancellationToken);
        }
    }

    private async Task OnChannelCheer(object sender, ChannelCheerArgs args)
    {
        Logger.LogInformation("Cheer: {User} cheered {Bits} bits",
            args.Notification.Payload.Event.IsAnonymous ? "Anonymous" : args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.Bits);
        
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.cheer",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );
        
        await _widgetEventService.PublishEventAsync("channel.cheer", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
            { "bits", args.Notification.Payload.Event.Bits.ToString() },
            { "isAnonymous", args.Notification.Payload.Event.IsAnonymous.ToString() },
            { "message", args.Notification.Payload.Event.Message }
        });

        string chatMessage = args.Notification.Payload.Event.IsAnonymous 
            ? $"An anonymous user just cheered {args.Notification.Payload.Event.Bits} bits! Thank you!"
            : $"@{args.Notification.Payload.Event.UserName} just cheered {args.Notification.Payload.Event.Bits} bits! Thank you!";

        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            chatMessage);

        // Send TTS if the message is not empty and the user is not anonymous
        bool widgetSubscriptions = await _widgetEventService.HasWidgetSubscriptionsAsync("channel.chat.message.tts");
        if (!string.IsNullOrEmpty(args.Notification.Payload.Event.Message) && 
            !args.Notification.Payload.Event.IsAnonymous && 
            widgetSubscriptions)
        {
            await _ttsService.SendCachedTts(args.Notification.Payload.Event.Message, args.Notification.Payload.Event.BroadcasterUserId, _cancellationToken);
        }
    }
    
    private async Task OnChannelAdBreakBegin(object sender, ChannelAdBreakBeginArgs args)
    {
        Logger.LogInformation("Ad break started for {Duration} seconds", args.Notification.Payload.Event.DurationSeconds);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.ad.break.begin",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await _widgetEventService.PublishEventAsync("channel.ad.break.begin", new Dictionary<string, string?>
        {
            { "channel", args.Notification.Payload.Event.BroadcasterUserLogin },
            { "duration", args.Notification.Payload.Event.DurationSeconds.ToString() }
        });

        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            $"An ad break has started for {args.Notification.Payload.Event.DurationSeconds.ToHumanTime()}. Please stay tuned!");
        
        // await _ttsService.SendCachedTts(
        //     "Attention chat: Jeff Bezos just checked his bank account and—surprise—he’s a few billion short for his next rocket. Please enjoy this ad break and help him reach orbit!",
        //     args.Notification.Payload.Event.BroadcasterUserId,
        //     CancellationToken.None);

        await Task.Delay(args.Notification.Payload.Event.DurationSeconds * 1000, CancellationToken.None);
        Logger.LogInformation("Ad break ended");

        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            "The ad break has ended. Thanks for your patience!");
    }
}

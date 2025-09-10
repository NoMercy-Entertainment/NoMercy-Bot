using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Services.Other;
using NoMercyBot.Services.Widgets;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace NoMercyBot.Services.Twitch.EventHandlers;

public class ChannelEventHandler : TwitchEventHandlerBase
{
    private readonly TwitchChatService _twitchChatService;
    private readonly TwitchApiService _twitchApiService;
    private readonly TtsService _ttsService;
    private readonly IWidgetEventService _widgetEventService;
    private readonly CancellationToken _cancellationToken;

    public ChannelEventHandler(
        AppDbContext dbContext,
        ILogger<ChannelEventHandler> logger,
        TwitchApiService twitchApiService,
        TtsService ttsService,
        TwitchChatService twitchChatService,
        IWidgetEventService widgetEventService,
        CancellationToken cancellationToken = default)
        : base(dbContext, logger, twitchApiService)
    {
        _twitchChatService = twitchChatService;
        _twitchApiService = twitchApiService;
        _widgetEventService = widgetEventService;
        _ttsService = ttsService;
        _cancellationToken = cancellationToken;
    }

    public override async Task RegisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient)
    {
        eventSubWebsocketClient.ChannelUpdate += OnChannelUpdate;
        eventSubWebsocketClient.ChannelFollow += OnChannelFollow;
        eventSubWebsocketClient.ChannelRaid += OnChannelRaid;
        eventSubWebsocketClient.ChannelBan += OnChannelBan;
        eventSubWebsocketClient.ChannelUnban += OnChannelUnban;
        eventSubWebsocketClient.ChannelModeratorAdd += OnChannelModeratorAdd;
        eventSubWebsocketClient.ChannelModeratorRemove += OnChannelModeratorRemove;
        await Task.CompletedTask;
    }

    public override async Task UnregisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient)
    {
        eventSubWebsocketClient.ChannelUpdate -= OnChannelUpdate;
        eventSubWebsocketClient.ChannelFollow -= OnChannelFollow;
        eventSubWebsocketClient.ChannelRaid -= OnChannelRaid;
        eventSubWebsocketClient.ChannelBan -= OnChannelBan;
        eventSubWebsocketClient.ChannelUnban -= OnChannelUnban;
        eventSubWebsocketClient.ChannelModeratorAdd -= OnChannelModeratorAdd;
        eventSubWebsocketClient.ChannelModeratorRemove -= OnChannelModeratorRemove;
        await Task.CompletedTask;
    }

    private async Task OnChannelUpdate(object sender, ChannelUpdateArgs args)
    {
        Logger.LogInformation("Channel updated");

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.update",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );

        await DbContext.ChannelInfo
            .Where(c => c.Id == args.Notification.Payload.Event.BroadcasterUserId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(c => c.Title, args.Notification.Payload.Event.Title)
                .SetProperty(c => c.Language, args.Notification.Payload.Event.Language)
                .SetProperty(c => c.GameId, args.Notification.Payload.Event.CategoryId)
                .SetProperty(c => c.GameName, args.Notification.Payload.Event.CategoryName)
                .SetProperty(c => c.ContentLabels, args.Notification.Payload.Event.ContentClassificationLabels.ToList())
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow), cancellationToken: _cancellationToken);
    }

    private async Task OnChannelFollow(object sender, ChannelFollowArgs args)
    {
        Logger.LogInformation("Follow: {User}", args.Notification.Payload.Event.UserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.follow",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );

        await _widgetEventService.PublishEventAsync("channel.follow", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName }
        });

        string message = $"Thanks for following, @{args.Notification.Payload.Event.UserName}! Welcome to the channel!";
        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin, message);
        
        bool widgetSubscriptions = await _widgetEventService.HasWidgetSubscriptionsAsync("channel.chat.message.tts");
        if (widgetSubscriptions)
        {
            await _ttsService.SendCachedTts(message, args.Notification.Payload.Event.BroadcasterUserId, _cancellationToken);
        }
    }

    private async Task OnChannelRaid(object sender, ChannelRaidArgs args)
    {
        Logger.LogInformation("Raid: {FromChannel} raided {ToChannel} with {Viewers} viewers",
            args.Notification.Payload.Event.FromBroadcasterUserLogin,
            args.Notification.Payload.Event.ToBroadcasterUserLogin,
            args.Notification.Payload.Event.Viewers);
        
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.raid",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.ToBroadcasterUserId,
            args.Notification.Payload.Event.FromBroadcasterUserId
        );

        await _widgetEventService.PublishEventAsync("channel.raid", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.FromBroadcasterUserName },
            { "viewers", args.Notification.Payload.Event.Viewers.ToString() }
        });
        
        if(args.Notification.Payload.Event.FromBroadcasterUserId == TwitchConfig.Service().UserId)
        {
            Logger.LogInformation("Raided out to {Channel}", args.Notification.Payload.Event.ToBroadcasterUserLogin);
            
            // TODO: Stop OBS broadcasting to Twitch.
            
            await _twitchApiService.SendAnnouncement(
                args.Notification.Payload.Event.FromBroadcasterUserId,
                args.Notification.Payload.Event.FromBroadcasterUserId,
                $"We have raided out to https://twitch.tv/{args.Notification.Payload.Event.ToBroadcasterUserName}, See you there!");
            
            return;
        }

        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.ToBroadcasterUserLogin,
            $"!so @{args.Notification.Payload.Event.FromBroadcasterUserName} just raided with {args.Notification.Payload.Event.Viewers} viewers! Welcome raiders!");
        
        bool widgetSubscriptions = await _widgetEventService.HasWidgetSubscriptionsAsync("channel.chat.message.tts");
        if (widgetSubscriptions)
        {
            await _ttsService.SendCachedTts($"{args.Notification.Payload.Event.FromBroadcasterUserName} just raided with {args.Notification.Payload.Event.Viewers} viewers! Welcome raiders!", args.Notification.Payload.Event.ToBroadcasterUserId, _cancellationToken);
        }
    }

    private async Task OnChannelBan(object sender, ChannelBanArgs args)
    {
        Logger.LogInformation("Ban: {User} was banned by {Moderator}. Reason: {Reason}",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.ModeratorUserLogin,
            args.Notification.Payload.Event.Reason);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.ban",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );

        await _widgetEventService.PublishEventAsync("channel.ban", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
            { "moderator", args.Notification.Payload.Event.ModeratorUserName },
            { "reason", args.Notification.Payload.Event.Reason }
        });

        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            $"@{args.Notification.Payload.Event.UserName} has been banned from the channel. Reason: {args.Notification.Payload.Event.Reason}");
    }

    private async Task OnChannelUnban(object sender, ChannelUnbanArgs args)
    {
        Logger.LogInformation("Unban: {User} was unbanned by {Moderator}",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.ModeratorUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.unban",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );

        await _widgetEventService.PublishEventAsync("channel.unban", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
            { "moderator", args.Notification.Payload.Event.ModeratorUserName }
        });

        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            $"@{args.Notification.Payload.Event.UserName} has been unbanned from the channel.");
    }

    private async Task OnChannelModeratorAdd(object sender, ChannelModeratorArgs args)
    {
        Logger.LogInformation("Mod add: {User} was modded", args.Notification.Payload.Event.UserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.moderator.add",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );

        await _widgetEventService.PublishEventAsync("channel.moderator.add", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
            { "broadcaster", args.Notification.Payload.Event.BroadcasterUserName }
        });

        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            $"@{args.Notification.Payload.Event.UserName} has been added as a moderator in the channel.");
    }

    private async Task OnChannelModeratorRemove(object sender, ChannelModeratorArgs args)
    {
        Logger.LogInformation("Mod remove: {User} was unmodded", args.Notification.Payload.Event.UserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.moderator.remove",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );

        await _widgetEventService.PublishEventAsync("channel.moderator.remove", new Dictionary<string, string?>
        {
            { "user", args.Notification.Payload.Event.UserName },
            { "broadcaster", args.Notification.Payload.Event.BroadcasterUserName }
        });

        await _twitchChatService.SendMessageAsBot(
            args.Notification.Payload.Event.BroadcasterUserLogin,
            $"@{args.Notification.Payload.Event.UserName} has been removed as a moderator in the channel.");
    }
}

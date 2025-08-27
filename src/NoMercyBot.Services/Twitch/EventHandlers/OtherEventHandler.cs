using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace NoMercyBot.Services.Twitch.EventHandlers;

public class OtherEventHandler : TwitchEventHandlerBase
{
    private readonly TwitchChatService _twitchChatService;

    public OtherEventHandler(
        AppDbContext dbContext,
        ILogger<OtherEventHandler> logger,
        TwitchApiService twitchApiService,
        TwitchChatService twitchChatService)
        : base(dbContext, logger, twitchApiService)
    {
        _twitchChatService = twitchChatService;
    }

    public override async Task RegisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient)
    {
        eventSubWebsocketClient.ChannelShieldModeBegin += OnChannelShieldModeBegin;
        eventSubWebsocketClient.ChannelShieldModeEnd += OnChannelShieldModeEnd;
        eventSubWebsocketClient.ChannelShoutoutCreate += OnShoutoutCreate;
        eventSubWebsocketClient.ChannelShoutoutReceive += OnShoutoutReceived;
        await Task.CompletedTask;
    }

    public override async Task UnregisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient)
    {
        eventSubWebsocketClient.ChannelShieldModeBegin -= OnChannelShieldModeBegin;
        eventSubWebsocketClient.ChannelShieldModeEnd -= OnChannelShieldModeEnd;
        eventSubWebsocketClient.ChannelShoutoutCreate -= OnShoutoutCreate;
        eventSubWebsocketClient.ChannelShoutoutReceive -= OnShoutoutReceived;
        await Task.CompletedTask;
    }

    private async Task OnChannelShieldModeBegin(object sender, ChannelShieldModeBeginArgs args)
    {
        Logger.LogInformation("Shield mode activated by {Moderator}",
            args.Notification.Payload.Event.ModeratorUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.shield.mode.begin",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.ModeratorUserId
        );
    }

    private async Task OnChannelShieldModeEnd(object sender, ChannelShieldModeEndArgs args)
    {
        Logger.LogInformation("Shield mode deactivated by {Moderator}",
            args.Notification.Payload.Event.ModeratorUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.shield.mode.end",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.ModeratorUserId
        );
    }

    private async Task OnShoutoutCreate(object sender, ChannelShoutoutCreateArgs args)
    {
        Logger.LogInformation("Shouted out {ToChannel}",
            args.Notification.Payload.Event.ToBroadcasterUserLogin);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.shoutout.create",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.ToBroadcasterUserId
        );
    }

    private async Task OnShoutoutReceived(object sender, ChannelShoutoutReceiveArgs args)
    {
        Logger.LogInformation(
            "Shoutout received from {FromChannel} with {ViewerCount} viewers",
            args.Notification.Payload.Event.FromBroadcasterUserLogin,
            args.Notification.Payload.Event.ViewerCount);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.shoutout.receive",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.FromBroadcasterUserId
        );

        _ = await TwitchApiService.GetOrFetchUser(args.Notification.Payload.Event.FromBroadcasterUserId);

        await _twitchChatService.SendOneOffMessage(
            args.Notification.Payload.Event.FromBroadcasterUserId,
            $"Thank you @{args.Notification.Payload.Event.FromBroadcasterUserName} for the shoutout, I appreciate it!"
        );
    }
}

using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace NoMercyBot.Services.Twitch.EventHandlers;

public class PollEventHandler : TwitchEventHandlerBase
{
    public PollEventHandler(
        AppDbContext dbContext,
        ILogger<PollEventHandler> logger,
        TwitchApiService twitchApiService)
        : base(dbContext, logger, twitchApiService)
    {
    }

    public override async Task RegisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient)
    {
        eventSubWebsocketClient.ChannelPollBegin += OnChannelPollBegin;
        eventSubWebsocketClient.ChannelPollProgress += OnChannelPollProgress;
        eventSubWebsocketClient.ChannelPollEnd += OnChannelPollEnd;
        await Task.CompletedTask;
    }

    public override async Task UnregisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient)
    {
        eventSubWebsocketClient.ChannelPollBegin -= OnChannelPollBegin;
        eventSubWebsocketClient.ChannelPollProgress -= OnChannelPollProgress;
        eventSubWebsocketClient.ChannelPollEnd -= OnChannelPollEnd;
        await Task.CompletedTask;
    }

    private async Task OnChannelPollBegin(object sender, ChannelPollBeginArgs args)
    {
        Logger.LogInformation("Poll started: \"{Title}\"", args.Notification.Payload.Event.Title);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.poll.begin",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );
    }

    private async Task OnChannelPollProgress(object sender, ChannelPollProgressArgs args)
    {
        Logger.LogInformation("Poll progress: \"{Title}\"", args.Notification.Payload.Event.Title);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.poll.progress",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );
    }

    private async Task OnChannelPollEnd(object sender, ChannelPollEndArgs args)
    {
        Logger.LogInformation("Poll ended: \"{Title}\". Status: {Status}",
            args.Notification.Payload.Event.Title,
            args.Notification.Payload.Event.Status);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.poll.end",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );
    }
}

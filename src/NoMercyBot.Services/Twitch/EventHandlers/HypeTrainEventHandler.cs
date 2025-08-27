using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace NoMercyBot.Services.Twitch.EventHandlers;

public class HypeTrainEventHandler : TwitchEventHandlerBase
{
    public HypeTrainEventHandler(
        AppDbContext dbContext,
        ILogger<HypeTrainEventHandler> logger,
        TwitchApiService twitchApiService)
        : base(dbContext, logger, twitchApiService)
    {
    }

    public override async Task RegisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient)
    {
        eventSubWebsocketClient.ChannelHypeTrainBeginV2 += OnHypeTrainBegin;
        eventSubWebsocketClient.ChannelHypeTrainProgressV2 += OnHypeTrainProgress;
        eventSubWebsocketClient.ChannelHypeTrainEndV2 += OnHypeTrainEnd;
        await Task.CompletedTask;
    }

    public override async Task UnregisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient)
    {
        eventSubWebsocketClient.ChannelHypeTrainBeginV2 -= OnHypeTrainBegin;
        eventSubWebsocketClient.ChannelHypeTrainProgressV2 -= OnHypeTrainProgress;
        eventSubWebsocketClient.ChannelHypeTrainEndV2 -= OnHypeTrainEnd;
        await Task.CompletedTask;
    }

    private async Task OnHypeTrainBegin(object sender, ChannelHypeTrainBeginV2Args args)
    {
        Logger.LogInformation("Hype Train started");

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.hype.train.begin",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );
    }

    private async Task OnHypeTrainProgress(object sender, ChannelHypeTrainProgressV2Args args)
    {
        Logger.LogInformation("Hype Train progress: Level {Level}, {Points}/{Goal} points",
            args.Notification.Payload.Event.Level,
            args.Notification.Payload.Event.Progress,
            args.Notification.Payload.Event.Goal);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.hype.train.progress",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );
    }

    private async Task OnHypeTrainEnd(object sender, ChannelHypeTrainEndV2Args args)
    {
        Logger.LogInformation("Hype Train ended. Reached Level {Level}",
            args.Notification.Payload.Event.Level);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.hype.train.end",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );
    }
}

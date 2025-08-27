using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace NoMercyBot.Services.Twitch.EventHandlers;

public class PredictionEventHandler : TwitchEventHandlerBase
{
    public PredictionEventHandler(
        AppDbContext dbContext,
        ILogger<PredictionEventHandler> logger,
        TwitchApiService twitchApiService)
        : base(dbContext, logger, twitchApiService)
    {
    }

    public override async Task RegisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient)
    {
        eventSubWebsocketClient.ChannelPredictionBegin += OnChannelPredictionBegin;
        eventSubWebsocketClient.ChannelPredictionProgress += OnChannelPredictionProgress;
        eventSubWebsocketClient.ChannelPredictionLock += OnChannelPredictionLock;
        eventSubWebsocketClient.ChannelPredictionEnd += OnChannelPredictionEnd;
        await Task.CompletedTask;
    }

    public override async Task UnregisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient)
    {
        eventSubWebsocketClient.ChannelPredictionBegin -= OnChannelPredictionBegin;
        eventSubWebsocketClient.ChannelPredictionProgress -= OnChannelPredictionProgress;
        eventSubWebsocketClient.ChannelPredictionLock -= OnChannelPredictionLock;
        eventSubWebsocketClient.ChannelPredictionEnd -= OnChannelPredictionEnd;
        await Task.CompletedTask;
    }

    private async Task OnChannelPredictionBegin(object sender, ChannelPredictionBeginArgs args)
    {
        Logger.LogInformation("Prediction started: \"{Title}\"", args.Notification.Payload.Event.Title);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.prediction.begin",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );
    }

    private async Task OnChannelPredictionProgress(object sender, ChannelPredictionProgressArgs args)
    {
        Logger.LogInformation("Prediction progress: \"{Title}\"", args.Notification.Payload.Event.Title);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.prediction.progress",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );
    }

    private async Task OnChannelPredictionLock(object sender, ChannelPredictionLockArgs args)
    {
        Logger.LogInformation("Prediction locked: \"{Title}\"", args.Notification.Payload.Event.Title);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.prediction.lock",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );
    }

    private async Task OnChannelPredictionEnd(object sender, ChannelPredictionEndArgs args)
    {
        Logger.LogInformation("Prediction ended: \"{Title}\". Status: {Status}",
            args.Notification.Payload.Event.Title,
            args.Notification.Payload.Event.Status);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.prediction.end",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );
    }
}

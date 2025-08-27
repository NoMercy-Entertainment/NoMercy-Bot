using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace NoMercyBot.Services.Twitch.EventHandlers;

public class ChannelPointsEventHandler : TwitchEventHandlerBase
{
    private readonly TwitchRewardService _twitchRewardService;

    public ChannelPointsEventHandler(
        AppDbContext dbContext,
        ILogger<ChannelPointsEventHandler> logger,
        TwitchApiService twitchApiService,
        TwitchRewardService twitchRewardService)
        : base(dbContext, logger, twitchApiService)
    {
        _twitchRewardService = twitchRewardService;
    }

    public override async Task RegisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient)
    {
        eventSubWebsocketClient.ChannelPointsCustomRewardAdd += OnChannelPointsCustomRewardAdd;
        eventSubWebsocketClient.ChannelPointsCustomRewardUpdate += OnChannelPointsCustomRewardUpdate;
        eventSubWebsocketClient.ChannelPointsCustomRewardRemove += OnChannelPointsCustomRewardRemove;
        eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionAdd += OnChannelPointsCustomRewardRedemptionAdd;
        eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionUpdate += OnChannelPointsCustomRewardRedemptionUpdate;
        await Task.CompletedTask;
    }

    public override async Task UnregisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient)
    {
        eventSubWebsocketClient.ChannelPointsCustomRewardAdd -= OnChannelPointsCustomRewardAdd;
        eventSubWebsocketClient.ChannelPointsCustomRewardUpdate -= OnChannelPointsCustomRewardUpdate;
        eventSubWebsocketClient.ChannelPointsCustomRewardRemove -= OnChannelPointsCustomRewardRemove;
        eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionAdd -= OnChannelPointsCustomRewardRedemptionAdd;
        eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionUpdate -= OnChannelPointsCustomRewardRedemptionUpdate;
        await Task.CompletedTask;
    }

    private async Task OnChannelPointsCustomRewardAdd(object sender, ChannelPointsCustomRewardArgs args)
    {
        Logger.LogInformation("Custom reward added: {Title}", args.Notification.Payload.Event.Title);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.points.custom.reward.add",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );
    }

    private async Task OnChannelPointsCustomRewardUpdate(object sender, ChannelPointsCustomRewardArgs args)
    {
        Logger.LogInformation("Custom reward updated: {Title}", args.Notification.Payload.Event.Title);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.points.custom.reward.update",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );
    }

    private async Task OnChannelPointsCustomRewardRemove(object sender, ChannelPointsCustomRewardArgs args)
    {
        Logger.LogInformation("Custom reward removed: {Title}", args.Notification.Payload.Event.Title);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.points.custom.reward.remove",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId
        );
    }

    private async Task OnChannelPointsCustomRewardRedemptionAdd(object sender, ChannelPointsCustomRewardRedemptionArgs args)
    {
        Logger.LogInformation("Reward redeemed: {User} redeemed {Title}",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.Reward.Title);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.points.custom.reward.redemption.add",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );

        await _twitchRewardService.ExecuteReward(args);
    }

    private async Task OnChannelPointsCustomRewardRedemptionUpdate(object sender, ChannelPointsCustomRewardRedemptionArgs args)
    {
        Logger.LogInformation("Reward redemption updated: {User}'s redemption of {Title} was {Status}",
            args.Notification.Payload.Event.UserLogin,
            args.Notification.Payload.Event.Reward.Title,
            args.Notification.Payload.Event.Status);

        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "channel.points.custom.reward.redemption.update",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.BroadcasterUserId,
            args.Notification.Payload.Event.UserId
        );
    }
}

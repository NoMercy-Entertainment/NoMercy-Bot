using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs.User;

namespace NoMercyBot.Services.Twitch.EventHandlers;

public class UserEventHandler : TwitchEventHandlerBase
{
    public UserEventHandler(
        AppDbContext dbContext,
        ILogger<UserEventHandler> logger,
        TwitchApiService twitchApiService)
        : base(dbContext, logger, twitchApiService)
    {
    }

    public override async Task RegisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient)
    {
        eventSubWebsocketClient.UserUpdate += OnUserUpdate;
        await Task.CompletedTask;
    }

    public override async Task UnregisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient)
    {
        eventSubWebsocketClient.UserUpdate -= OnUserUpdate;
        await Task.CompletedTask;
    }

    private async Task OnUserUpdate(object sender, UserUpdateArgs args)
    {
        Logger.LogInformation("User updated: {User}", args.Notification.Payload.Event);
        
        await SaveChannelEvent(
            args.Notification.Metadata.MessageId,
            "user.update",
            args.Notification.Payload.Event,
            args.Notification.Payload.Event.UserId
        );

        User user = await TwitchApiService.FetchUser(id: args.Notification.Payload.Event.UserId);

        await DbContext.Users
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

        Logger.LogInformation("Updated user info for {User}", args.Notification.Payload.Event.UserLogin);
    }
}

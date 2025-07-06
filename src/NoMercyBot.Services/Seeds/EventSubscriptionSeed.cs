using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Globals.Information;

namespace NoMercyBot.Services.Seeds;

public static class EventSubscriptionSeed
{
    public static async Task SeedEventSubscriptions(this AppDbContext dbContext)
    {
        if (await dbContext.EventSubscriptions.AnyAsync())
            return;

        List<EventSubscription> subscriptions = [];
        
        // Add Twitch events
        AddTwitchEvents(subscriptions);
        
        // Add Discord events
        AddDiscordEvents(subscriptions);
        
        // Add OBS events
        AddObsEvents(subscriptions);

        // Add all subscriptions to database
        await dbContext.EventSubscriptions.AddRangeAsync(subscriptions);
        await dbContext.SaveChangesAsync();
    }
    
    private static void AddTwitchEvents(List<EventSubscription> subscriptions)
    {
        // Twitch events, all disabled by default
        string[] twitchEvents =
        [
            "channel.update",
            "channel.follow",
            "channel.subscribe",
            "channel.subscription.gift",
            "channel.subscription.message",
            "channel.cheer", 
            "channel.raid",
            "channel.chat.message",
            "stream.online",
            "stream.offline",
            "channel.hype_train.begin",
            "channel.hype_train.progress",
            "channel.hype_train.end",
            "channel.poll.begin",
            "channel.poll.progress",
            "channel.poll.end",
            "channel.prediction.begin",
            "channel.prediction.progress",
            "channel.prediction.lock",
            "channel.prediction.end",
            "channel.charity_campaign.donate",
            "channel.charity_campaign.start",
            "channel.charity_campaign.progress",
            "channel.charity_campaign.stop",
            "channel.shield_mode.begin",
            "channel.shield_mode.end",
            "channel.shoutout.create",
            "channel.shoutout.receive"
        ];

        string callbackUrl = $"http://localhost:{Config.InternalServerPort}/api/eventsub/twitch";
        
        foreach (string eventType in twitchEvents)
        {
            subscriptions.Add(new("twitch", eventType, false)
            {
                CallbackUrl = callbackUrl
            });
        }
    }

    private static void AddDiscordEvents(List<EventSubscription> subscriptions)
    {
        // Discord events, all disabled by default
        string[] discordEvents =
        [
            "guild.create",
            "guild.delete",
            "guild.member_add",
            "guild.member_remove",
            "message.create",
            "message.delete",
            "voice.state_update",
            "interaction",
            "ready",
            "channel.create",
            "channel.delete",
            "channel.pins_update",
            "guild.ban_add",
            "guild.ban_remove",
            "guild.emojis_update",
            "guild.integrations_update",
            "guild.role_create",
            "guild.role_delete",
            "guild.role_update"
        ];

        string callbackUrl = $"http://localhost:{Config.InternalServerPort}/api/eventsub/discord";
        
        foreach (string eventType in discordEvents)
        {
            subscriptions.Add(new("discord", eventType, false)
            {
                CallbackUrl = callbackUrl
            });
        }
    }

    private static void AddObsEvents(List<EventSubscription> subscriptions)
    {
        // OBS events, all disabled by default
        string[] obsEvents =
        [
            "scene.changed",
            "stream.started",
            "stream.stopped",
            "recording.started",
            "recording.stopped",
            "source.visibility.changed",
            "media.started",
            "media.ended",
            "scene.item.added",
            "scene.item.removed",
            "scene.item.visibility.changed",
            "scene.collection.changed",
            "exit.started",
            "recording.paused",
            "recording.resumed",
            "streaming.status",
            "virtual.cam.started",
            "virtual.cam.stopped"
        ];

        foreach (string eventType in obsEvents)
        {
            subscriptions.Add(new("obs", eventType, false));
        }
    }
}

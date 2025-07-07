using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Services.Twitch;

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
        foreach (KeyValuePair<string, (string Description, string Version, string[] Condition)> eventItem in TwitchEventSubService.AvailableEventTypes)
        {
            subscriptions.Add(new("twitch", eventItem.Key, false, eventItem.Value.Version)
            {
                Description = eventItem.Value.Description,
                Condition = eventItem.Value.Condition,
            });
        }
    }

    private static void AddDiscordEvents(List<EventSubscription> subscriptions)
    {
        // Dictionary of Discord events with their descriptions
        Dictionary<string, string> discordEvents = new()
        {
            { "guild.create", "When the bot joins a new Discord server" },
            { "guild.delete", "When the bot leaves or is removed from a Discord server" },
            { "guild.member_add", "When a new member joins a Discord server" },
            { "guild.member_remove", "When a member leaves or is removed from a Discord server" },
            { "message.create", "When a message is sent in a channel" },
            { "message.delete", "When a message is deleted" },
            { "voice.state_update", "When a user joins, leaves, or moves between voice channels" },
            { "interaction", "When a user interacts with a bot command or component" },
            { "ready", "When the bot has successfully connected to Discord" },
            { "channel.create", "When a new channel is created" },
            { "channel.delete", "When a channel is deleted" },
            { "channel.pins_update", "When a message is pinned or unpinned in a channel" },
            { "guild.ban_add", "When a user is banned from a server" },
            { "guild.ban_remove", "When a user is unbanned from a server" },
            { "guild.emojis_update", "When a server's emoji list is updated" },
            { "guild.integrations_update", "When a guild integration is updated" },
            { "guild.role_create", "When a role is created in a server" },
            { "guild.role_delete", "When a role is deleted from a server" },
            { "guild.role_update", "When a role's settings are updated" }
        };
        
        foreach (KeyValuePair<string, string> eventItem in discordEvents)
        {
            subscriptions.Add(new("discord", eventItem.Key, false)
            {
                Description = eventItem.Value
            });
        }
    }

    private static void AddObsEvents(List<EventSubscription> subscriptions)
    {
        // Dictionary of OBS events with their descriptions
        Dictionary<string, string> obsEvents = new()
        {
            { "scene.changed", "When the active scene in OBS is changed" },
            { "stream.started", "When streaming begins in OBS" },
            { "stream.stopped", "When streaming ends in OBS" },
            { "recording.started", "When recording begins in OBS" },
            { "recording.stopped", "When recording ends in OBS" },
            { "source.visibility.changed", "When a source's visibility is toggled in OBS" },
            { "media.started", "When media playback begins in OBS" },
            { "media.ended", "When media playback ends in OBS" },
            { "scene.item.added", "When an item is added to a scene in OBS" },
            { "scene.item.removed", "When an item is removed from a scene in OBS" },
            { "scene.item.visibility.changed", "When an item's visibility is toggled in a scene" },
            { "scene.collection.changed", "When the scene collection is changed in OBS" },
            { "exit.started", "When OBS begins to shut down" },
            { "recording.paused", "When recording is paused in OBS" },
            { "recording.resumed", "When recording is resumed in OBS" },
            { "streaming.status", "When the streaming status changes in OBS" },
            { "virtual.cam.started", "When the virtual camera is started in OBS" },
            { "virtual.cam.stopped", "When the virtual camera is stopped in OBS" }
        };
        
        foreach (KeyValuePair<string, string> eventItem in obsEvents)
        {
            subscriptions.Add(new("obs", eventItem.Key, false)
            {
                Description = eventItem.Value,
            });
        }
    }
}

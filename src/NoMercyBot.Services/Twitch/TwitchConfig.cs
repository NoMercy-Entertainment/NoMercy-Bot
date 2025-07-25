﻿using NoMercyBot.Database.Models;
using NoMercyBot.Globals.Information;
using NoMercyBot.Services.Interfaces;

namespace NoMercyBot.Services.Twitch;

public class TwitchConfig : IConfig
{
    internal static Service? _service;

    public static Service Service()
    {
        return _service ??= new();
    }

    public bool IsEnabled => Service().Enabled;

    public static string ApiUrl { get; } = "https://api.twitch.tv/helix";
    public static string AuthUrl { get; } = "https://id.twitch.tv/oauth2";

    public static string RedirectUri => $"http://localhost:{Config.InternalClientPort}/oauth/twitch/callback";

    public static readonly Dictionary<string, string> AvailableScopes = new()
    {
        { "analytics:read:extensions", "View analytics data for the Twitch Extensions owned by the authenticated account." },
        { "analytics:read:games", "View analytics data for the games owned by the authenticated account." },
        { "bits:read", "View Bits information for a channel." },
        { "channel:bot", "Joins your channel's chatroom as a bot user, and perform chat-related actions as that user." },
        { "channel:manage:ads", "Manage ads schedule on a channel." },
        { "channel:read:ads", "Read the ads schedule and details on your channel." },
        { "channel:manage:broadcast", "Manage a channel's broadcast configuration, including updating channel configuration and managing stream markers and stream tags." },
        { "channel:read:charity", "Read charity campaign details and user donations on your channel." },
        { "channel:edit:commercial", "Run commercials on a channel." },
        { "channel:read:editors", "View a list of users with the editor role for a channel." },
        { "channel:manage:extensions", "Manage a channel's Extension configuration, including activating Extensions." },
        { "channel:read:goals", "View Creator Goals for a channel." },
        { "channel:read:guest_star", "Read Guest Star details for your channel." },
        { "channel:manage:guest_star", "Manage Guest Star for your channel." },
        { "channel:read:hype_train", "View Hype Train information for a channel." },
        { "channel:manage:moderators", "Add or remove the moderator role from users in your channel." },
        { "channel:read:polls", "View a channel's polls." },
        { "channel:manage:polls", "Manage a channel's polls." },
        { "channel:read:predictions", "View a channel's SimpleChannel Points Predictions." },
        { "channel:manage:predictions", "Manage of channel's SimpleChannel Points Predictions" },
        { "channel:manage:raids", "Manage a channel raiding another channel." },
        { "channel:read:redemptions", "View SimpleChannel Points custom rewards and their redemptions on a channel." },
        { "channel:manage:redemptions", "Manage SimpleChannel Points custom rewards and their redemptions on a channel." },
        { "channel:manage:schedule", "Manage a channel's stream schedule." },
        { "channel:read:stream_key", "View an authorized user's stream key." },
        { "channel:read:subscriptions", "View a list of all subscribers to a channel and check if a user is subscribed to a channel." },
        { "channel:manage:videos", "Manage a channel's videos, including deleting videos." },
        { "channel:read:vips", "Read the list of VIPs in your channel." },
        { "channel:manage:vips", "Add or remove the VIP role from users in your channel." },
        { "channel:moderate", "Perform moderation actions in a channel." },
        { "clips:edit", "Manage Clips for a channel." },
        { "moderation:read", "View a channel's moderation data including Moderators, Bans, Timeouts, and Automod settings." },
        { "moderator:manage:announcements", "Send announcements in channels where you have the moderator role." },
        { "moderator:manage:automod", "Manage messages held for review by AutoMod in channels where you are a moderator." },
        { "moderator:read:automod_settings", "View a broadcaster's AutoMod settings." },
        { "moderator:manage:automod_settings", "Manage a broadcaster's AutoMod settings." },
        { "moderator:read:banned_users", "Read the list of bans or unbans in channels where you have the moderator role." },
        { "moderator:manage:banned_users", "Ban and unban users." },
        { "moderator:read:blocked_terms", "View a broadcaster's list of blocked terms." },
        { "moderator:read:chat_messages", "Read deleted chat messages in channels where you have the moderator role." },
        { "moderator:manage:blocked_terms", "Manage a broadcaster's list of blocked terms." },
        { "moderator:manage:chat_messages", "Delete chat messages in channels where you have the moderator role" },
        { "moderator:read:chat_settings", "View a broadcaster's chat room settings." },
        { "moderator:manage:chat_settings", "Manage a broadcaster's chat room settings." },
        { "moderator:read:chatters", "View the chatters in a broadcaster's chat room." },
        { "moderator:read:followers", "Read the followers of a broadcaster." },
        { "moderator:read:guest_star", "Read Guest Star details for channels where you are a Guest Star moderator." },
        { "moderator:manage:guest_star", "Manage Guest Star for channels where you are a Guest Star moderator." },
        { "moderator:read:moderators", "Read the list of moderators in channels where you have the moderator role." },
        { "moderator:read:shield_mode", "View a broadcaster's Shield Mode status." },
        { "moderator:manage:shield_mode", "Manage a broadcaster's Shield Mode status." },
        { "moderator:read:shoutouts", "View a broadcaster's shoutouts." },
        { "moderator:manage:shoutouts", "Manage a broadcaster's shoutouts." },
        { "moderator:read:suspicious_users", "Read chat messages from suspicious users and see users flagged as suspicious in channels where you have the moderator role." },
        { "moderator:read:unban_requests", "View a broadcaster's unban requests." },
        { "moderator:manage:unban_requests", "Manage a broadcaster's unban requests." },
        { "moderator:read:vips", "Read the list of VIPs in channels where you have the moderator role." },
        { "moderator:read:warnings", "Read warnings in channels where you have the moderator role." },
        { "moderator:manage:warnings", "Warn users in channels where you have the moderator role." },
        { "user:bot", "Join a specified chat channel as your user and appear as a bot, and perform chat-related actions as your user." },
        { "user:edit", "Manage a user object." },
        { "user:edit:broadcast", "View and edit a user's broadcasting configuration, including Extension configurations." },
        { "user:read:blocked_users", "View the block list of a user." },
        { "user:manage:blocked_users", "Manage the block list of a user." },
        { "user:read:broadcast", "View a user's broadcasting configuration, including Extension configurations." },
        { "user:read:chat", "Receive chatroom messages and informational notifications relating to a channel's chatroom." },
        { "user:manage:chat_color", "Update the color used for the user's name in chat." },
        { "user:read:email", "View a user's email address." },
        { "user:read:emotes", "View emotes available to a user" },
        { "user:read:follows", "View the list of channels a user follows." },
        { "user:read:moderated_channels", "Read the list of channels you have moderator privileges in." },
        { "user:read:subscriptions", "View if an authorized user is subscribed to specific channels." },
        { "user:read:whispers", "Receive whispers sent to your user." },
        { "user:manage:whispers", "Receive whispers sent to your user, and send whispers on your user's behalf." },
        { "user:write:chat", "Send chat messages to a chatroom." },
        { "chat:edit", "Send chat messages to a chatroom using an IRC connection." },
        { "chat:read", "View chat messages sent in a chatroom using an IRC connection." },
        { "whispers:read", "Receive whisper messages for your user using PubSub.}" }
    };
}
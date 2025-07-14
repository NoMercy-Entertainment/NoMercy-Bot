
namespace NoMercyBot.Services;

public class BotConfig
{
    public static readonly Dictionary<string, string> AvailableScopes = new()
    {
        { "channel:bot", "Joins your channel's chatroom as a bot user, and perform chat-related actions as that user." },
        { "chat:edit", "Send chat messages to a chatroom using an IRC connection." },
        { "chat:read", "View chat messages sent in a chatroom using an IRC connection." },
        { "moderator:manage:automod", "Manage messages held for review by AutoMod in channels where you are a moderator." },
        { "moderator:read:automod_settings", "View a broadcaster's AutoMod settings." },
        { "moderator:manage:automod_settings", "Manage a broadcaster's AutoMod settings." },
    };
}
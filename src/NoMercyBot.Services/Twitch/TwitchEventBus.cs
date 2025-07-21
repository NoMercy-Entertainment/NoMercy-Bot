using NoMercyBot.Database.Models.ChatMessage;

namespace NoMercyBot.Services.Twitch;

public class TwitchEventBus
{
    public event EventHandler<ChatMessage>? ChatMessageReceived;

    public void RaiseChatMessage(ChatMessage message)
    {
        ChatMessageReceived?.Invoke(this, message);
    }
}
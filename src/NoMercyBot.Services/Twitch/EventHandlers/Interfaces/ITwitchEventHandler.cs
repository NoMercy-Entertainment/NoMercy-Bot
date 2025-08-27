using TwitchLib.EventSub.Websockets;

namespace NoMercyBot.Services.Twitch.EventHandlers.Interfaces;

public interface ITwitchEventHandler
{
    Task RegisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient);
    Task UnregisterEventHandlersAsync(EventSubWebsocketClient eventSubWebsocketClient);
}

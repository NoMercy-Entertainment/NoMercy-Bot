using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NoMercyBot.Services.Spotify;

namespace NoMercyBot.Services.Widgets;

public class WidgetHub : Hub
{
    private readonly ILogger<WidgetHub> _logger;
    private readonly IWidgetEventService _widgetEventService;
    private readonly SpotifyApiService _spotifyApiService;

    public WidgetHub(ILogger<WidgetHub> logger,
        SpotifyApiService spotifyApiService,
        IWidgetEventService widgetEventService)
    {
        _logger = logger;
        _widgetEventService = widgetEventService;
        _spotifyApiService = spotifyApiService;
    }

    public async Task JoinWidgetGroup(string widgetId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"widget-{widgetId}");
        _logger.LogDebug("Connection {ConnectionId} joined widget group {WidgetId}", Context.ConnectionId, widgetId);

        await Task.Delay(5000).ContinueWith(async _ =>
        {
            await _widgetEventService.PublishEventAsync("spotify.state.changed", _spotifyApiService.SpotifyState);
        });
    }

    public async Task LeaveWidgetGroup(string widgetId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"widget-{widgetId}");
        _logger.LogDebug("Connection {ConnectionId} left widget group {WidgetId}", Context.ConnectionId, widgetId);
    }

    public async Task NotifyServerShutdown()
    {
        _logger.LogInformation("Notifying all widget connections of server shutdown");
        await Clients.All.SendAsync("ServerShutdown");
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogDebug("Widget connection established: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("Widget connection disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
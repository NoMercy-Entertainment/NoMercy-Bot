using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace NoMercyBot.Services.Widgets;

public class WidgetHub : Hub
{
    private readonly ILogger<WidgetHub> _logger;

    public WidgetHub(ILogger<WidgetHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinWidgetGroup(string widgetId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"widget-{widgetId}");
        _logger.LogDebug("Connection {ConnectionId} joined widget group {WidgetId}", Context.ConnectionId, widgetId);
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

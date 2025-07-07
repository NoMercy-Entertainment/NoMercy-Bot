using Microsoft.Extensions.Logging;

namespace NoMercyBot.Services.Twitch;

/// <summary>
/// Mediator service that allows event subscription services to communicate without direct references
/// </summary>
public class EventSubscriptionMediator
{
    private readonly ILogger<EventSubscriptionMediator> _logger;
    
    public delegate Task EventSubscriptionChangedHandler(string eventType, bool enabled);
    
    public event EventSubscriptionChangedHandler? OnEventSubscriptionChanged;
    
    public EventSubscriptionMediator(ILogger<EventSubscriptionMediator> logger)
    {
        _logger = logger;
    }
    
    public async Task NotifyEventSubscriptionChanged(string eventType, bool enabled)
    {
        _logger.LogInformation($"Event subscription changed: {eventType} is now {(enabled ? "enabled" : "disabled")}");
        
        if (OnEventSubscriptionChanged != null)
        {
            await OnEventSubscriptionChanged(eventType, enabled);
        }
    }
}

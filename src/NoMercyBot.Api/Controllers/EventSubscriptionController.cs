using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Database.Models;
using NoMercyBot.Services;
using NoMercyBot.Services.Discord;
using NoMercyBot.Services.Obs;
using NoMercyBot.Services.Twitch;

namespace NoMercyBot.Api.Controllers;

[ApiController]
[Tags("EventSubscriptions")]
[Route("api/settings/events")]
public class EventSubscriptionController : BaseController
{
    private readonly Dictionary<string, IEventSubService> _eventSubServices;
    
    public EventSubscriptionController(
        [FromServices] TwitchEventSubService twitchEventSubService,
        [FromServices] DiscordEventSubService discordEventSubService,
        [FromServices] ObsEventSubService obsEventSubService
    )
    {
        _eventSubServices = new()
        {
            ["twitch"] = twitchEventSubService,
            ["discord"] = discordEventSubService,
            ["obs"] = obsEventSubService
        };
    }

    private IActionResult GetEventSubService(string provider, out IEventSubService? service)
    {
        service = null;

        if (!_eventSubServices.TryGetValue(provider.ToLower(), out IEventSubService? foundService))
        {
            return NotFoundResponse($"Provider '{provider}' not found");
        }

        service = foundService;
        return Ok();
    }

    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        List<string> providers = _eventSubServices.Keys.ToList();
        return Ok(providers);
    }

    [HttpGet("types/{provider}")]
    public IActionResult GetAvailableEventTypes(string provider)
    {
        IActionResult serviceResult = GetEventSubService(provider, out IEventSubService? service);
        if (serviceResult is not OkResult) return serviceResult;

        IEnumerable<string> eventTypes = service!.GetAvailableEventTypes();
        return Ok(eventTypes);
    }

    [HttpGet("{provider}")]
    public async Task<IActionResult> GetSubscriptions(string provider)
    {
        IActionResult serviceResult = GetEventSubService(provider, out IEventSubService? service);
        if (serviceResult is not OkResult) return serviceResult;

        List<EventSubscription> subscriptions = await service!.GetAllSubscriptionsAsync();
        return Ok(subscriptions);
    }

    [HttpGet("{provider}/{id}")]
    public async Task<IActionResult> GetSubscription(string provider, string id)
    {
        IActionResult serviceResult = GetEventSubService(provider, out IEventSubService? service);
        if (serviceResult is not OkResult) return serviceResult;

        EventSubscription? subscription = await service!.GetSubscriptionAsync(id);
        if (subscription == null)
            return NotFoundResponse($"Subscription with ID '{id}' not found");

        return Ok(subscription);
    }

    [HttpPost("{provider}")]
    public async Task<IActionResult> CreateSubscription(string provider, [FromBody] CreateSubscriptionRequest request)
    {
        if (string.IsNullOrEmpty(request.EventType))
            return BadRequestResponse("EventType is required");

        IActionResult serviceResult = GetEventSubService(provider, out IEventSubService? service);
        if (serviceResult is not OkResult) return serviceResult;

        try
        {
            EventSubscription subscription = await service!.CreateSubscriptionAsync(request.EventType, request.Enabled);
            return CreatedAtAction(
                nameof(GetSubscription),
                new { provider, id = subscription.Id },
                subscription);
        }
        catch (ArgumentException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse($"Failed to create subscription: {ex.Message}");
        }
    }

    [HttpPut("{provider}/{id}")]
    public async Task<IActionResult> UpdateSubscription(string provider, string id, [FromBody] UpdateSubscriptionRequest request)
    {
        IActionResult serviceResult = GetEventSubService(provider, out IEventSubService? service);
        if (serviceResult is not OkResult) return serviceResult;

        try
        {
            await service!.UpdateSubscriptionAsync(id, request.Enabled);
            EventSubscription? updated = await service.GetSubscriptionAsync(id);
            
            if (updated == null)
                return NotFoundResponse($"Subscription with ID '{id}' not found");
                
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResponse(ex.Message);
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse($"Failed to update subscription: {ex.Message}");
        }
    }

    [HttpDelete("{provider}/{id}")]
    public async Task<IActionResult> DeleteSubscription(string provider, string id)
    {
        IActionResult serviceResult = GetEventSubService(provider, out IEventSubService? service);
        if (serviceResult is not OkResult) return serviceResult;

        try
        {
            await service!.DeleteSubscriptionAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse($"Failed to delete subscription: {ex.Message}");
        }
    }

    [HttpDelete("{provider}")]
    public async Task<IActionResult> DeleteAllSubscriptions(string provider)
    {
        IActionResult serviceResult = GetEventSubService(provider, out IEventSubService? service);
        if (serviceResult is not OkResult) return serviceResult;

        try
        {
            bool success = await service!.DeleteAllSubscriptionsAsync();
            if (success)
                return NoContent();
            else
                return InternalServerErrorResponse("Failed to delete all subscriptions");
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse($"Failed to delete subscriptions: {ex.Message}");
        }
    }
}

public class CreateSubscriptionRequest
{
    public string EventType { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

public class UpdateSubscriptionRequest
{
    public bool Enabled { get; set; }
}

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NoMercyBot.Services;
using NoMercyBot.Services.Discord;
using NoMercyBot.Services.Obs;
using NoMercyBot.Services.Twitch;

namespace NoMercyBot.Api.Controllers;

[ApiController]
[Route("api/eventsub/{provider}")]
public class EventSubController : BaseController
{
    private readonly ILogger<EventSubController> _logger;
    private readonly Dictionary<string, IEventSubService> _eventSubServices;

    public EventSubController(
        ILogger<EventSubController> logger,
        [FromServices] TwitchEventSubService twitchEventSubService,
        [FromServices] DiscordEventSubService discordEventSubService,
        [FromServices] ObsEventSubService obsEventSubService)
    {
        _logger = logger;
        _eventSubServices = new()
        {
            ["twitch"] = twitchEventSubService,
            ["discord"] = discordEventSubService,
            ["obs"] = obsEventSubService
        };
    }

    private IActionResult GetEventSubService([FromRoute] string provider, out IEventSubService? service)
    {
        service = null;

        if (!_eventSubServices.TryGetValue(provider.ToLower(), out IEventSubService? foundService))
        {
            return NotFoundResponse($"Provider '{provider}' not found");
        }

        service = foundService;
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> EventSubNotification([FromRoute] string provider)
    {
        try
        {
            IActionResult serviceResult = GetEventSubService(provider, out IEventSubService? eventSubService);
            if (serviceResult is not OkResult) return serviceResult;

            // Enable re-reading the request body
            HttpContext.Request.EnableBuffering();

            // Read the request body
            string payload;
            using (StreamReader reader = new(Request.Body, Encoding.UTF8, true, 1024, true))
            {
                payload = await reader.ReadToEndAsync();
            }
            Request.Body.Position = 0;

            // Log the received notification
            _logger.LogDebug("Received {Provider} notification: {Payload}", provider, payload);

            // Verify the signature
            if (!eventSubService!.VerifySignature(HttpContext.Request, payload))
            {
                _logger.LogWarning("Invalid signature for {Provider} notification", provider);
                return Unauthorized();
            }

            // Extract event type from appropriate header or payload based on provider
            string eventType = provider.ToLower() switch
            {
                "twitch" => Request.Headers.TryGetValue("Twitch-Eventsub-Subscription-Type", out StringValues type) 
                    ? type.ToString() : "unknown",
                "discord" => Request.Headers.TryGetValue("X-Discord-Event-Type", out StringValues type) 
                    ? type.ToString() : "interaction",
                "obs" => Request.Headers.TryGetValue("Obs-Event-Type", out StringValues type) 
                    ? type.ToString() : "unknown",
                _ => "unknown"
            };

            // Handle the event
            return Ok(await eventSubService.HandleEventAsync(HttpContext.Request, payload, eventType));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Provider} notification", provider);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Services;
using NoMercyBot.Services.Discord;
using NoMercyBot.Services.Obs;
using NoMercyBot.Services.Spotify;
using NoMercyBot.Services.Twitch;

namespace NoMercyBot.Api.Controllers;

[ApiController]
[Tags("Providers")]
[Route("api/settings/providers")]
public class ServiceController : BaseController
{
    private readonly AppDbContext _dbContext;
    private readonly ServiceResolver _serviceResolver;
    private readonly Dictionary<string, IAuthService> _authServices;
    
    public ServiceController(
        AppDbContext dbContext, 
        ServiceResolver serviceResolver,
        [FromServices] TwitchAuthService twitchAuthService,
        [FromServices] SpotifyAuthService spotifyAuthService,
        [FromServices] DiscordAuthService discordAuthService,
        [FromServices] ObsAuthService obsAuthService
    )
    {
        _dbContext = dbContext;
        _serviceResolver = serviceResolver;
        _authServices = new()
        {
            ["twitch"] = twitchAuthService,
            ["spotify"] = spotifyAuthService,
            ["discord"] = discordAuthService,
            ["obs"] = obsAuthService
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetProviders()
    {
        List<Service> providers = await _dbContext.Services.ToListAsync();
        
        if (providers.Count == 0)
            return NotFoundResponse("No providers found");
        
        foreach (Service provider in providers)
        {
            _authServices.TryGetValue(provider.Name.ToLower(), out IAuthService? foundService);
            provider.AvailableScopes = foundService?.AvailableScopes ?? [];
        }
        
        return Ok(providers);
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> GetService(string name)
    {
        Service? service = await _dbContext.Services
            .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());
            
        if (service == null)
            return NotFoundResponse($"Service '{name}' not found");

        _authServices.TryGetValue(name.ToLower(), out IAuthService? foundService);
        service.AvailableScopes = foundService?.AvailableScopes ?? [];
            
        return Ok(service);
    }

    [HttpPut("{provider}")]
    public async Task<IActionResult> UpdateService(string provider, [FromBody] ServiceUpdateRequest request)
    {
        Service? service = await _dbContext.Services
            .FirstOrDefaultAsync(s => s.Name.ToLower() == provider.ToLower());
            
        if (service == null)
            return NotFoundResponse($"Service '{provider}' not found");
            
        service.Enabled = request.Enabled;
        service.ClientId = request.ClientId;
        service.ClientSecret = request.ClientSecret;
        
        if (request.Scopes != null && request.Scopes.Length > 0)
            service.Scopes = request.Scopes;
        
        await _dbContext.SaveChangesAsync();
        
        // Reload service configurations
        await _serviceResolver.InitializeAllServices();
        
        return Ok(service);
    }

    [HttpPut("{provider}/status")]
    public async Task<IActionResult> UpdateServiceStatus(string provider, [FromBody] ServiceStatusUpdateRequest request)
    {
        Service? service = await _dbContext.Services
            .FirstOrDefaultAsync(s => s.Name.ToLower() == provider.ToLower());
            
        if (service == null)
            return NotFoundResponse($"Service '{provider}' not found");
            
        service.Enabled = request.Enabled;
        await _dbContext.SaveChangesAsync();
        
        // Reload service configurations
        await _serviceResolver.InitializeAllServices();
        
        return Ok(new { name = service.Name, enabled = service.Enabled });
    }
}

public class ServiceUpdateRequest
{
    public bool Enabled { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string[]? Scopes { get; set; }
}

public class ServiceStatusUpdateRequest
{
    public bool Enabled { get; set; }
}
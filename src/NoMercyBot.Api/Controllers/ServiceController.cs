using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Services;

namespace NoMercyBot.Api.Controllers;

[ApiController]
[Tags("Providers")]
[Route("api/settings/providers")]
public class ServiceController : BaseController
{
    private readonly AppDbContext _dbContext;
    private readonly ServiceResolver _serviceResolver;

    public ServiceController(AppDbContext dbContext, ServiceResolver serviceResolver)
    {
        _dbContext = dbContext;
        _serviceResolver = serviceResolver;
    }

    [HttpGet]
    public async Task<IActionResult> GetProviders()
    {
        List<Service> providers = await _dbContext.Services.ToListAsync();
        return Ok(providers);
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> GetService(string name)
    {
        Service? service = await _dbContext.Services
            .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());
            
        if (service == null)
            return NotFoundResponse($"Service '{name}' not found");
            
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
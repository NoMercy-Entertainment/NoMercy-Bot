using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Services.Twitch.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMercyBot.Services.Twitch;

namespace NoMercyBot.Api.Controllers;

[ApiController]
[Route("api/bot")]
[Tags("Bot")]
public class BotAuthController : BaseController
{
    private readonly AppDbContext _dbContext;
    private readonly BotAuthService _botAuthService;
    private readonly TwitchApiService _twitchApiService;
    private readonly ILogger<BotAuthController> _logger;

    public BotAuthController(
        AppDbContext dbContext,
        TwitchApiService twitchApiService,
        BotAuthService botAuthService,
        ILogger<BotAuthController> logger)
    {
        _dbContext = dbContext;
        _botAuthService = botAuthService;
        _twitchApiService = twitchApiService;
        _logger = logger;
    }

    [HttpGet("authenticate")]
    public async Task<IActionResult> Authenticate()
    {
        try
        {
            // Use device code flow
            DeviceCodeResponse deviceCodeResponse = await _botAuthService.Authorize();
            
            return Ok(deviceCodeResponse);
        }
        catch (Exception ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }

    [HttpPost("device/token")]
    public async Task<IActionResult> GetTokenFromDeviceCode([FromBody] DeviceCodeRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.DeviceCode))
                return BadRequestResponse("Device code is required");

            // Use the TwitchAuthService to poll for the token
            TokenResponse tokenResponse = await _botAuthService.PollForToken(request.DeviceCode);
            
            // Get user information using the token
            User? user;
            try 
            {
                user = await _twitchApiService.FetchUser();
                _logger.LogInformation("Successfully fetched user: {Username}", user.DisplayName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user information");
                return BadRequestResponse($"Error fetching user information: {ex.Message}");
            }
            
            // Store the bot account
            BotAccount? botAccount = await _dbContext.BotAccounts.FirstOrDefaultAsync();
            if (botAccount == null)
            {
                _logger.LogInformation("Creating new bot account for user {Username}", user.DisplayName);
                botAccount = new()
                {
                    Username = user.Username,
                    ClientId = _botAuthService.ClientId,
                    ClientSecret = _botAuthService.ClientSecret,
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
                };
                _dbContext.BotAccounts.Add(botAccount);
            }
            else
            {
                _logger.LogInformation("Updating existing bot account for user {Username}", user.DisplayName);
                botAccount.Username = user.Username;
                botAccount.AccessToken = tokenResponse.AccessToken;
                botAccount.RefreshToken = tokenResponse.RefreshToken;
                botAccount.TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            }
            
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Bot account saved successfully");

            return Ok(new
            {
                success = true, 
                username = user.DisplayName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get token from device code");
            return BadRequestResponse($"Failed to get token: {ex.Message}");
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetAuthStatus()
    {
        try
        {
            BotAccount? botAccount = await _dbContext.BotAccounts.FirstOrDefaultAsync();
            
            if (botAccount == null)
            {
                return Ok(new { authenticated = false });
            }

            bool isValid = true;
            string username = botAccount.Username;
            
            // Check if token is expired
            if (botAccount.TokenExpiry.HasValue && botAccount.TokenExpiry < DateTime.UtcNow)
            {
                try
                {
                    // Try refreshing the token
                    (User user, TokenResponse tokenResponse) = await _botAuthService.RefreshToken(botAccount.RefreshToken);
                    
                    // Update the bot account
                    botAccount.AccessToken = tokenResponse.AccessToken;
                    botAccount.RefreshToken = tokenResponse.RefreshToken;
                    botAccount.TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                    
                    await _dbContext.SaveChangesAsync();
                    
                    username = user.DisplayName;
                }
                catch
                {
                    isValid = false;
                }
            }
            
            return Ok(new { 
                authenticated = isValid, 
                username = username,
                tokenExpiry = botAccount.TokenExpiry
            });
        }
        catch (Exception ex)
        {
            return BadRequestResponse($"Failed to get status: {ex.Message}");
        }
    }
}

public class DeviceCodeRequest
{
    public string DeviceCode { get; set; } = string.Empty;
}

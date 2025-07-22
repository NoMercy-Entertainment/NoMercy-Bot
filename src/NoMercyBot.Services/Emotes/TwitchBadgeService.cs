using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using RestSharp;
using Newtonsoft.Json;
using Microsoft.Extensions.Hosting;
using NoMercyBot.Database.Models.ChatMessage;
using NoMercyBot.Services.Emotes.Dto;
using NoMercyBot.Services.Twitch;

namespace NoMercyBot.Services.Emotes;

public class TwitchBadgeService : IHostedService
{
    private readonly RestClient _client;
    private readonly IServiceScope _scope;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<TwitchBadgeService> _logger;
    private readonly TwitchAuthService _twitchAuthService;
    public List<ChatBadge> TwitchBadges { get; private set; } = [];

    public TwitchBadgeService(IServiceScopeFactory serviceScopeFactory, ILogger<TwitchBadgeService> logger, TwitchAuthService twitchAuthService)
    {
        _scope = serviceScopeFactory.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _logger = logger;
        _twitchAuthService = twitchAuthService;
        _client = new("https://api.twitch.tv/helix/chat/badges");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Twitch badge service initialization");
        try
        {
            await Initialize();
            _logger.LogInformation("Twitch badge service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Twitch badge service, but continuing startup");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task Initialize()
    {
        _logger.LogInformation("Initializing Twitch badges cache...");
        try
        {
            await GetGlobalBadges();
            await GetChannelBadges(_twitchAuthService.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Twitch badges");
        }
    }

    private async Task GetGlobalBadges()
    {
        try
        {
            _logger.LogInformation("Fetching global Twitch badges");

            RestRequest request = new("/global");
            request.AddHeader("Authorization", $"Bearer {_twitchAuthService.Service.AccessToken}");
            request.AddHeader("Client-Id", _twitchAuthService.ClientId);

            RestResponse response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || response.Content == null)
                throw new("Failed to fetch global Twitch badges");

            TwitchGlobalBadgesResponse? result = JsonConvert.DeserializeObject<TwitchGlobalBadgesResponse>(response.Content);
            if (result?.Data == null)
                throw new("No global Twitch badges found");
            
            foreach (TwitchGlobalBadgesResponseData badge in result.Data)
            {
                foreach (TwitchGlobalBadgesVersion version in badge.Versions)
                {
                    TwitchBadges.Add(new()
                    {
                        SetId = badge.SetId,
                        Id = version.Id,
                        Info = version.Description,
                        Urls = new()
                        {
                            { "1", version.ImageUrl1X },
                            { "2", version.ImageUrl2X },
                            { "4", version.ImageUrl4X }
                        }
                    });
                }
            }

            _logger.LogInformation($"Loaded {TwitchBadges.Count} global Twitch badges");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching global Twitch badges");
        }
    }

    private async Task GetChannelBadges(string broadcasterId)
    {
        try
        {
            RestRequest request = new($"?broadcaster_id={broadcasterId}");
            request.AddHeader("Authorization", $"Bearer {_twitchAuthService.Service.AccessToken}");
            request.AddHeader("Client-Id", _twitchAuthService.ClientId);

            RestResponse response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || response.Content == null)
                return;

            TwitchGlobalBadgesResponse? channelBadges = JsonConvert.DeserializeObject<TwitchGlobalBadgesResponse>(response.Content);
            if (channelBadges != null)
            {
                foreach (TwitchGlobalBadgesResponseData badge in channelBadges.Data)
                {
                    foreach (TwitchGlobalBadgesVersion version in badge.Versions)
                    {
                        TwitchBadges.Add(new()
                        {
                            SetId = badge.SetId,
                            Id = version.Id,
                            Info = version.Title,
                            Urls = new()
                            {
                                { "1", version.ImageUrl1X },
                                { "2", version.ImageUrl2X },
                                { "4", version.ImageUrl4X }
                            }
                        });
                    }
                }
                _logger.LogInformation($"Loaded {channelBadges.Data.Length} channel Twitch badges for {broadcasterId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading channel Twitch badges for {broadcasterId}: {ex.Message}");
        }
    }
}

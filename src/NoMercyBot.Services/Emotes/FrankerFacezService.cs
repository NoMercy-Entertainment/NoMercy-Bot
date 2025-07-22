using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using RestSharp;
using Newtonsoft.Json;
using NoMercyBot.Services.Emotes.Dto;
using Microsoft.Extensions.Hosting;
using NoMercyBot.Services.Twitch;

namespace NoMercyBot.Services.Emotes;

public class FrankerFacezService : IHostedService
{
    private readonly RestClient _client;
    private readonly IServiceScope _scope;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<FrankerFacezService> _logger;
    private readonly TwitchAuthService _twitchAuthService;
    public List<Emoticon> FrankerFacezEmotes { get; private set; } = [];

    public FrankerFacezService(IServiceScopeFactory serviceScopeFactory,  ILogger<FrankerFacezService> logger, TwitchAuthService twitchAuthService)
    {;
        _scope = serviceScopeFactory.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _logger = logger;
        _twitchAuthService = twitchAuthService;
        _client = new("https://api.frankerfacez.com/v1/");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting FrankerFacez emote service initialization");
        try
        {
            // Run initialization in background so it doesn't block startup
            _ = Task.Run(async () => 
            {
                try
                {
                    await Initialize();
                    _logger.LogInformation("FrankerFacez emote service initialized successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize FrankerFacez emotes, but continuing startup");
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting FrankerFacez emote service, but continuing startup");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task Initialize()
    {
        _logger.LogInformation("Initializing FrankerFacez emotes cache...");
        try
        {
            await GetGlobalEmotes();
            await GetChannelEmotes(_twitchAuthService.UserName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get global FrankerFacez emotes");
        }
    }

    private async Task GetGlobalEmotes()
    {
        try
        {
            RestRequest request = new("set/global");
            RestResponse response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || response.Content == null)
                throw new("Failed to fetch global FFZ emotes");

            FrankerFacezResponse? obj = JsonConvert.DeserializeObject<FrankerFacezResponse>(response.Content);
            
            foreach (int setId in obj?.DefaultSets ?? [])
                if (obj?.Sets.TryGetValue(setId.ToString(), out FrankerFacezSet? set) ?? false)
                    foreach (Emoticon emote in set.Emoticons)
                        FrankerFacezEmotes.Add(emote);
                        
            _logger.LogInformation($"Loaded {FrankerFacezEmotes.Count} global FFZ emotes");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading global FFZ emotes: {ex.Message}");
        }
    }

    private async Task GetChannelEmotes(string channelName)
    {
        try
        {
            RestRequest request = new($"room/{channelName}");
            RestResponse response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || response.Content == null)
                return;

            FrankerFacezResponse? frankerFacezResponse = JsonConvert.DeserializeObject<FrankerFacezResponse>(response.Content);
            
            if (frankerFacezResponse?.Sets != null)
            {
                foreach (FrankerFacezSet set in frankerFacezResponse.Sets.Values)
                    foreach (Emoticon emote in set.Emoticons)
                        FrankerFacezEmotes.Add(emote);
                
                _logger.LogInformation($"Loaded {frankerFacezResponse.Sets.Count} FFZ sets for channel {channelName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading channel FFZ emotes for {channelName}: {ex.Message}");
        }
    }
}

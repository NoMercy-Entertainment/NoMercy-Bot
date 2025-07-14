using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using RestSharp;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using NoMercyBot.Services.Emotes.Dto;
using Microsoft.Extensions.Hosting;

namespace NoMercyBot.Services.Emotes;

public class BttvService : IHostedService
{
    private readonly RestClient _client;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<BttvService> _logger;
    public BttvEmote[]? BttvEmotes;
    private readonly ConcurrentDictionary<string, ChannelBttvEmotesResponse> _getChannelBttvCache = new();

    public BttvService(AppDbContext dbContext, ILogger<BttvService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _client = new("https://api.betterttv.net/3/");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting BTTV emote service initialization");
        try
        {
            // Run initialization in background so it doesn't block startup
            _ = Task.Run(async () => 
            {
                try
                {
                    await Initialize();
                    _logger.LogInformation("BTTV emote service initialized successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize BTTV emotes, but continuing startup");
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting BTTV emote service, but continuing startup");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task Initialize()
    {
        _logger.LogInformation("Initializing BTTV emotes cache...");
        try
        {
            await GetGlobalEmotes();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get global BTTV emotes");
            // Initialize empty array to prevent null reference exceptions
            BttvEmotes = Array.Empty<BttvEmote>();
        }
    }

    public async Task<BttvEmote[]> GetGlobalEmotes()
    {
        try
        {
            _logger.LogInformation("Fetching global BTTV emotes");

            if (BttvEmotes != null)
                return BttvEmotes;

            RestRequest request = new("cached/emotes/global");
            RestResponse response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || response.Content == null)
                throw new Exception("Failed to fetch global BTTV emotes");

            BttvEmotes = JsonConvert.DeserializeObject<BttvEmote[]>(response.Content);
            _logger.LogInformation($"Loaded {BttvEmotes?.Length ?? 0} global BTTV emotes");
            return BttvEmotes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching global BTTV emotes");
            return Array.Empty<BttvEmote>();
        }
    }

    public async Task<ChannelBttvEmotesResponse?> GetChannelEmotes(string broadcasterId)
    {
        if (_getChannelBttvCache.TryGetValue(broadcasterId, out ChannelBttvEmotesResponse? cached))
            return cached;

        try
        {
            RestRequest request = new($"cached/users/twitch/{broadcasterId}");
            RestResponse response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || response.Content == null)
                return null;

            ChannelBttvEmotesResponse? result = JsonConvert.DeserializeObject<ChannelBttvEmotesResponse>(response.Content);
            if (result != null)
            {
                _getChannelBttvCache.AddOrUpdate(broadcasterId, result, (_, _) => result);
                _logger.LogInformation($"Loaded {result.ChannelEmotes.Length + result.SharedEmotes.Length} channel BTTV emotes for {broadcasterId}");
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading channel BTTV emotes for {broadcasterId}: {ex.Message}");
            return null;
        }
    }

    public void ResetChannelCache(string broadcasterId)
    {
        _getChannelBttvCache.TryRemove(broadcasterId, out _);
    }
}

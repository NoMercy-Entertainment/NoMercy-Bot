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
        await Initialize();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task Initialize()
    {
        _logger.LogInformation("Initializing BTTV emotes cache...");
        await GetGlobalEmotes();
    }

    public async Task<BttvEmote[]?> GetGlobalEmotes()
    {
        if (BttvEmotes != null)
            return BttvEmotes;

        try
        {
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
            _logger.LogError($"Error loading global BTTV emotes: {ex.Message}");
            return null;
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


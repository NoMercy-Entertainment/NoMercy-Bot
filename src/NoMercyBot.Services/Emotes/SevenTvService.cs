using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NoMercyBot.Database;
using NoMercyBot.Services.Emotes.Dto;
using RestSharp;
using Microsoft.Extensions.Hosting;

namespace NoMercyBot.Services.Emotes;

public class SevenTvService : IHostedService
{
    private readonly RestClient _client;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SevenTvService> _logger;
    public SevenTvEmote[]? SevenTvEmotes;
    private readonly ConcurrentDictionary<string, SevenTvEmote[]> _getChannel7TvCache = new();

    public SevenTvService(AppDbContext dbContext, ILogger<SevenTvService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _client = new("https://7tv.io/v3/");
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
        _logger.LogInformation("Initializing 7TV emotes cache...");
        await GetGlobalEmotes();
    }

    public async Task<SevenTvEmote[]?> GetGlobalEmotes()
    {
        if (SevenTvEmotes != null)
            return SevenTvEmotes;

        try
        {
            RestRequest request = new("emote-sets/global");
            RestResponse response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || response.Content == null)
                throw new Exception("Failed to fetch global 7TV emotes");

            SevenTvGlobalResponse? obj = JsonConvert.DeserializeObject<SevenTvGlobalResponse>(response.Content);
            
            List<SevenTvEmote> list = [];
            foreach (SevenTvEmote emote in obj?.Emotes ?? [])
                list.Add(emote);
                
            SevenTvEmotes = list.ToArray();
            _logger.LogInformation($"Loaded {SevenTvEmotes.Length} global 7TV emotes");
            return SevenTvEmotes;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading global 7TV emotes: {ex.Message}");
            return null;
        }
    }

    public async Task<SevenTvEmote[]?> GetChannelEmotes(string broadcasterId)
    {
        if (_getChannel7TvCache.TryGetValue(broadcasterId, out SevenTvEmote[]? cached))
            return cached;

        try
        {
            RestRequest request = new($"users/twitch/{broadcasterId}");
            RestResponse response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || response.Content == null)
                return null;

            SevenTvGlobalResponse? obj = JsonConvert.DeserializeObject<SevenTvGlobalResponse>(response.Content);
            
            List<SevenTvEmote> list = [];
            if (obj?.Emotes != null)
            {
                foreach (SevenTvEmote emote in obj.Emotes)
                    list.Add(emote);
            }
            
            SevenTvEmote[] result = list.ToArray();
            _getChannel7TvCache.AddOrUpdate(broadcasterId, result, (_, _) => result);
            _logger.LogInformation($"Loaded {result.Length} channel 7TV emotes for {broadcasterId}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading channel 7TV emotes for {broadcasterId}: {ex.Message}");
            return null;
        }
    }

    public void ResetChannelCache(string broadcasterId)
    {
        _getChannel7TvCache.TryRemove(broadcasterId, out _);
    }
}
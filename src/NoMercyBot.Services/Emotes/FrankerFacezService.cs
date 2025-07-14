using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using RestSharp;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using NoMercyBot.Services.Emotes.Dto;
using Microsoft.Extensions.Hosting;

namespace NoMercyBot.Services.Emotes;

public class FrankerFacezService : IHostedService
{
    private readonly RestClient _client;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<FrankerFacezService> _logger;
    public Emoticon[]? FrankerFacezEmotes;
    private readonly ConcurrentDictionary<string, Emoticon[]> _getChannelFfzCache = new();

    public FrankerFacezService(AppDbContext dbContext, ILogger<FrankerFacezService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _client = new("https://api.frankerfacez.com/v1/");
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
        _logger.LogInformation("Initializing FrankerFacez emotes cache...");
        await GetGlobalEmotes();
    }

    public async Task<Emoticon[]?> GetGlobalEmotes()
    {
        if (FrankerFacezEmotes != null)
            return FrankerFacezEmotes;

        try
        {
            RestRequest request = new("set/global");
            RestResponse response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || response.Content == null)
                throw new Exception("Failed to fetch global FFZ emotes");

            FrankerFacezResponse? obj = JsonConvert.DeserializeObject<FrankerFacezResponse>(response.Content);
            
            List<Emoticon> list = [];
            foreach (int setId in obj?.DefaultSets ?? [])
                if (obj?.Sets.TryGetValue(setId.ToString(), out FrankerFacezSet? set) ?? false)
                    foreach (Emoticon emote in set.Emoticons)
                        list.Add(emote);
                        
            FrankerFacezEmotes = list.ToArray();
            _logger.LogInformation($"Loaded {FrankerFacezEmotes.Length} global FFZ emotes");
            return FrankerFacezEmotes;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading global FFZ emotes: {ex.Message}");
            return null;
        }
    }

    public async Task<Emoticon[]?> GetChannelEmotes(string channelName)
    {
        if (_getChannelFfzCache.TryGetValue(channelName, out Emoticon[]? cached))
            return cached;

        try
        {
            RestRequest request = new($"room/{channelName}");
            RestResponse response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || response.Content == null)
                return null;

            FrankerFacezResponse? frankerFacezResponse = JsonConvert.DeserializeObject<FrankerFacezResponse>(response.Content);
            
            List<Emoticon> list = [];
            if (frankerFacezResponse?.Sets != null)
            {
                foreach (FrankerFacezSet set in frankerFacezResponse.Sets.Values)
                    foreach (Emoticon emote in set.Emoticons)
                        list.Add(emote);
            }
            
            Emoticon[] result = list.ToArray();
            _getChannelFfzCache.AddOrUpdate(channelName, result, (_, _) => result);
            _logger.LogInformation($"Loaded {result.Length} channel FFZ emotes for {channelName}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading channel FFZ emotes for {channelName}: {ex.Message}");
            return null;
        }
    }

    public void ResetChannelCache(string channelName)
    {
        _getChannelFfzCache.TryRemove(channelName, out _);
    }
}

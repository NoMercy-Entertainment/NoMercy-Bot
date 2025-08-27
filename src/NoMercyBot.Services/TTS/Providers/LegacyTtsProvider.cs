using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database;
using NoMercyBot.Globals.NewtonSoftConverters;
using NoMercyBot.Services.TTS.Models;
using RestSharp;
using NoMercyBot.Globals.SystemCalls;
using Serilog.Events;
using DatabaseTtsVoice = NoMercyBot.Database.Models.TtsVoice;

namespace NoMercyBot.Services.TTS.Providers;

public class LegacyTtsProvider : TtsProviderBase, IDisposable
{
    private const string SpeakerInfoFile = "Assets/speaker-info.json";
    private readonly RestClient _client;
    private readonly AppDbContext _dbContext;

    public LegacyTtsProvider(AppDbContext dbContext)
        : base("Legacy", "legacy", true, 0)
    {
        _dbContext = dbContext;
        _client = new("http://localhost:6040");
    }

    public string Name => "Legacy";
    public bool IsAvailable => true;

    public override async Task<byte[]> SynthesizeAsync(string text, string voiceId,
        CancellationToken cancellationToken = default)
    {
        // For now, return empty audio bytes to prevent blocking
        // You can implement actual TTS synthesis here later
        await Task.Delay(100, cancellationToken); // Simulate processing
        return new byte[1024]; // Return minimal audio data to prevent null reference
    }

    public override async Task<bool> IsAvailableAsync()
    {
        try
        {
            RestRequest request = new("api/health");
            RestResponse response = await _client.ExecuteAsync(request);
            return response.IsSuccessful;
        }
        catch
        {
            return false;
        }
    }

    public override async Task<List<TtsVoice>> GetAvailableVoicesAsync()
    {
        // First check if we have voices in the database (after seeding)
        List<DatabaseTtsVoice> dbVoices = await _dbContext.TtsVoices
            .AsNoTracking()
            .Where(v => v.Provider == "Legacy")
            .ToListAsync();

        if (dbVoices.Count > 0)
            return dbVoices.Select(v => new TtsVoice
            {
                Id = v.SpeakerId,
                Name = v.Name,
                DisplayName = v.DisplayName,
                Locale = v.Locale,
                Gender = v.Gender,
                Provider = "Legacy",
                IsDefault = v.IsDefault
            }).ToList();

        // Fallback to reading from JSON file for seeding
        return await LoadVoicesFromJsonFile();
    }

    private async Task<List<TtsVoice>> LoadVoicesFromJsonFile()
    {
        if (!File.Exists(SpeakerInfoFile))
        {
            Logger.Setup($"Legacy speaker info file not found at {SpeakerInfoFile}", LogEventLevel.Warning);
            return [];
        }

        try
        {
            string json = await File.ReadAllTextAsync(SpeakerInfoFile);
            List<SpeakerInfoJson> legacyVoices = json.FromJson<List<SpeakerInfoJson>>() ?? [];

            return legacyVoices.Select(voice => new TtsVoice
            {
                Id = voice.Id,
                Name = voice.Name,
                DisplayName = voice.Name,
                Locale = "en-US",
                Gender = voice.Gender,
                Provider = "Legacy",
                IsDefault = false
            }).ToList();
        }
        catch (Exception ex)
        {
            Logger.Setup($"Error reading legacy voices from JSON file: {ex.Message}", LogEventLevel.Warning);
            return [];
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    private class SpeakerInfoJson
    {
        public string Id { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Accent { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }

    public override Task<int> GetCharacterCountAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(0);

        return Task.FromResult(text.Length);
    }

    public override Task<decimal> CalculateCostAsync(string text, string voiceId)
    {
        // Legacy TTS provider is free - no cost
        return Task.FromResult(0m);
    }

    public override Task<string> GetDefaultVoiceIdAsync()
    {
        // Return the default legacy voice (ED\n is the fallback from the original code)
        return Task.FromResult("ED\n");
    }
}
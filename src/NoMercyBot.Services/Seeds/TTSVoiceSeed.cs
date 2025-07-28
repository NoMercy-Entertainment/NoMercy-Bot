using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database;
using Newtonsoft.Json;
using NoMercyBot.Database.Models;
using NoMercyBot.Globals.SystemCalls;
using Serilog.Events;

namespace NoMercyBot.Services.Seeds;

public static class TtsVoiceSeed
{
    private const string SpeakerInfoFile = "Assets/speaker-info.json";

    public static async Task Init(AppDbContext dbContext)
    {
        Logger.Setup("Starting TTSVoiceSeed");
        try
        {
            if (!File.Exists(SpeakerInfoFile))
            {
                Logger.Setup($"Speaker info file not found at {SpeakerInfoFile}", LogEventLevel.Warning);
                return;
            }

            string json = await File.ReadAllTextAsync(SpeakerInfoFile);
            List<SpeakerInfoJson> voices = JsonConvert.DeserializeObject<List<SpeakerInfoJson>>(json) ?? [];
            
            List<TtsVoice> newVoices = voices
                .Select(v => new TtsVoice
                {
                    Id = v.Id,
                    Name = v.Name,
                    Gender = v.Gender,
                    Age = v.Age,
                    Accent = v.Accent,
                    Region = v.Region,
                })
                .ToList();
            
            if (newVoices.Count > 0)
            {
                await dbContext.TtsVoices.UpsertRange(newVoices)
                    .On(v => v.Id)
                    .WhenMatched((e, incoming) => new()
                        {
                            Name = incoming.Name,
                            Gender = incoming.Gender,
                            Age = incoming.Age,
                            Accent = incoming.Accent,
                            Region = incoming.Region,
                        })
                    .RunAsync();
                
                Logger.Setup($"Seeded {newVoices.Count} new TTS voices.", LogEventLevel.Verbose);
            }
            else
            {
                Logger.Setup("No new TTS voices to seed.");
            }
        }
        catch (Exception ex)
        {
            Logger.Setup("Error seeding TTS voices", LogEventLevel.Error);
        }
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
}


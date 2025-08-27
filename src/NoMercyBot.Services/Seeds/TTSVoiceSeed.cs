using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database;
using NoMercyBot.Globals.SystemCalls;
using NoMercyBot.Services.TTS.Interfaces;
using Serilog.Events;
using DatabaseTtsVoice = NoMercyBot.Database.Models.TtsVoice;
using ServicesTtsVoice = NoMercyBot.Services.TTS.Models.TtsVoice;

namespace NoMercyBot.Services.Seeds;

public static class TtsVoiceSeed
{
    public static async Task Init(AppDbContext dbContext, IEnumerable<ITtsProvider> ttsProviders)
    {
        Logger.Setup("Starting TTSVoiceSeed");
        try
        {
            List<DatabaseTtsVoice> allVoices = [];

            // Get voices from all available providers
            foreach (ITtsProvider provider in ttsProviders)
                try
                {
                    await provider.InitializeAsync();

                    if (!await provider.IsAvailableAsync())
                    {
                        Logger.Setup($"Provider '{provider.Name}' is not available, skipping voice seeding");
                        continue;
                    }

                    List<ServicesTtsVoice> providerVoices = await provider.GetAvailableVoicesAsync();
                    List<DatabaseTtsVoice> convertedVoices =
                        ConvertProviderVoicesToDatabaseVoices(providerVoices, provider.Name);

                    allVoices.AddRange(convertedVoices);
                    Logger.Setup($"Retrieved {providerVoices.Count} voices from provider '{provider.Name}'");
                }
                catch (Exception ex)
                {
                    Logger.Setup($"Error retrieving voices from provider '{provider.Name}': {ex.Message}",
                        LogEventLevel.Warning);
                }

            if (allVoices.Count > 0)
            {
                await dbContext.TtsVoices.UpsertRange(allVoices)
                    .On(v => v.Id)
                    .WhenMatched((existing, incoming) => new()
                    {
                        SpeakerId = incoming.SpeakerId,
                        Name = incoming.Name,
                        DisplayName = incoming.DisplayName,
                        Locale = incoming.Locale,
                        Gender = incoming.Gender,
                        Age = incoming.Age,
                        Accent = incoming.Accent,
                        Region = incoming.Region,
                        Provider = incoming.Provider,
                    })
                    .RunAsync();

                Logger.Setup($"Successfully seeded {allVoices.Count} TTS voices from all providers");
            }
            else
            {
                Logger.Setup("No TTS voices found to seed from any provider", LogEventLevel.Warning);
            }
        }
        catch (Exception ex)
        {
            Logger.Setup($"Error seeding TTS voices: {ex.Message}", LogEventLevel.Error);
        }
    }

    private static List<DatabaseTtsVoice> ConvertProviderVoicesToDatabaseVoices(
        List<ServicesTtsVoice> providerVoices,
        string providerName)
    {
        return providerVoices.Select(voice => new DatabaseTtsVoice
        {
            Id = $"{providerName}:{voice.Id}",
            SpeakerId = voice.Id,
            Name = voice.Name,
            DisplayName = !string.IsNullOrWhiteSpace(voice.DisplayName)
                ? voice.DisplayName
                : voice.Name,
            Locale = voice.Locale,
            Gender = voice.Gender,
            Age = 0,
            Accent = string.Empty,
            Region = voice.Locale.Contains('-')
                ? voice.Locale.Split('-')[1].ToUpperInvariant()
                : string.Empty,
            Provider = providerName,
            IsDefault = voice.IsDefault,
            IsActive = true
        }).ToList();
    }
}
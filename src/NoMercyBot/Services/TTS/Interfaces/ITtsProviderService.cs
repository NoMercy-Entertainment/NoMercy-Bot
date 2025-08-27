using NoMercyBot.Services.TTS.Services;

namespace NoMercyBot.Services.TTS.Interfaces;

public interface ITtsProviderService
{
    /// <summary>
    /// Gets the best available TTS provider that can handle the character count WITHOUT exceeding monthly limits
    /// </summary>
    Task<ITtsProvider?> GetBestAvailableProviderAsync(int characterCount);
    
    /// <summary>
    /// Gets the best available TTS provider WITHOUT checking character limits - used for cached content and voice selection
    /// </summary>
    Task<ITtsProvider?> GetBestAvailableProviderIgnoringLimitsAsync();
    
    /// <summary>
    /// Gets all available TTS providers with their current usage status
    /// </summary>
    Task<List<TtsProviderStatus>> GetProviderStatusAsync();
}

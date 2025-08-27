using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Globals.Information;

namespace NoMercyBot.Services.TTS.Services;

public class TtsCacheService
{
    private readonly AppDbContext _dbContext;

    public TtsCacheService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Generates a SHA256 hash from text content and voice ID for cache key
    /// </summary>
    public string GenerateContentHash(string textContent, string voiceId)
    {
        string combined = $"{textContent}|{voiceId}";
        byte[] bytes = Encoding.UTF8.GetBytes(combined);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Checks if a cached TTS file exists for the given text and voice combination
    /// </summary>
    public async Task<TtsCacheEntry?> GetCachedEntryAsync(string textContent, string voiceId,
        CancellationToken cancellationToken = default)
    {
        string contentHash = GenerateContentHash(textContent, voiceId);

        TtsCacheEntry? cacheEntry = await _dbContext.TtsCacheEntries
            .FirstOrDefaultAsync(x => x.ContentHash == contentHash, cancellationToken);

        if (cacheEntry != null)
        {
            // Check if the file still exists on disk
            if (File.Exists(cacheEntry.FilePath))
            {
                // Update access tracking
                cacheEntry.LastAccessedAt = DateTime.UtcNow;
                cacheEntry.AccessCount++;
                await _dbContext.SaveChangesAsync(cancellationToken);

                return cacheEntry;
            }
            else
            {
                // File no longer exists, remove from cache
                _dbContext.TtsCacheEntries.Remove(cacheEntry);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a new cache entry for synthesized TTS audio
    /// </summary>
    public async Task<TtsCacheEntry> CreateCacheEntryAsync(
        string textContent,
        string voiceId,
        string provider,
        byte[] audioBytes,
        decimal cost,
        CancellationToken cancellationToken = default)
    {
        string contentHash = GenerateContentHash(textContent, voiceId);
        string fileName = $"tts_cache_{contentHash}.wav";
        string filePath = Path.Combine(AppFiles.CachePath, "tts", "cached", fileName);

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);

        // Write audio file to disk
        await File.WriteAllBytesAsync(filePath, audioBytes, cancellationToken);

        // Create cache entry in database
        TtsCacheEntry cacheEntry = new()
        {
            ContentHash = contentHash,
            TextContent = textContent,
            VoiceId = voiceId,
            Provider = provider,
            FilePath = filePath,
            FileSize = audioBytes.Length,
            CharacterCount = textContent.Length,
            Cost = cost,
            LastAccessedAt = DateTime.UtcNow,
            AccessCount = 1
        };

        _dbContext.TtsCacheEntries.Add(cacheEntry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return cacheEntry;
    }

    /// <summary>
    /// Gets the file path for a cached TTS entry
    /// </summary>
    public string GetCacheFilePath(string contentHash)
    {
        string fileName = $"tts_cache_{contentHash}.wav";
        return Path.Combine(AppFiles.CachePath, "tts", "cached", fileName);
    }

    /// <summary>
    /// Cleans up old cache entries based on age and access frequency
    /// </summary>
    public async Task CleanupOldCacheEntriesAsync(TimeSpan maxAge, int minAccessCount = 1,
        CancellationToken cancellationToken = default)
    {
        DateTime cutoffDate = DateTime.UtcNow - maxAge;

        List<TtsCacheEntry> oldEntries = await _dbContext.TtsCacheEntries
            .Where(x => x.LastAccessedAt < cutoffDate && x.AccessCount < minAccessCount)
            .ToListAsync(cancellationToken);

        foreach (TtsCacheEntry entry in oldEntries)
        {
            // Delete file from disk if it exists
            if (File.Exists(entry.FilePath))
                try
                {
                    File.Delete(entry.FilePath);
                }
                catch
                {
                    // Ignore file deletion errors
                }

            // Remove from database
            _dbContext.TtsCacheEntries.Remove(entry);
        }

        if (oldEntries.Count > 0) await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
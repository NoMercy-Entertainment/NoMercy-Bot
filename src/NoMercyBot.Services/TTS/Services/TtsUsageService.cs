using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Services.TTS.Interfaces;

namespace NoMercyBot.Services.TTS.Services;

public class TtsUsageService : ITtsUsageService
{
    private readonly AppDbContext _dbContext;

    public TtsUsageService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> CanUseCharactersAsync(string providerId, int characterCount)
    {
        // Check if temporary override is active
        if (await HasTemporaryOverrideAsync())
            return true;

        // Get current usage and limits
        int currentUsage = await GetCurrentUsageAsync(providerId);
        int characterLimit = await GetCharacterLimitAsync(providerId);

        return currentUsage + characterCount <= characterLimit;
    }

    public async Task<TtsUsageRecord> RecordUsageAsync(string providerId, int characterCount, decimal cost)
    {
        DateTime billingPeriodStart = await GetCurrentBillingPeriodStartAsync();
        DateTime billingPeriodEnd = await GetCurrentBillingPeriodEndAsync();

        TtsUsageRecord usageRecord = new()
        {
            Id = Ulid.NewUlid().ToString(),
            ProviderId = providerId.ToLowerInvariant(),
            CharactersUsed = characterCount,
            BillingPeriodStart = billingPeriodStart,
            BillingPeriodEnd = billingPeriodEnd,
            Cost = cost,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TtsUsageRecords.Add(usageRecord);
        await _dbContext.SaveChangesAsync();

        return usageRecord;
    }

    public async Task<int> GetCurrentUsageAsync(string providerId)
    {
        DateTime startOfMonth = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        DateTime endOfMonth = startOfMonth.AddMonths(1);

        return await _dbContext.TtsUsageRecords
            .Where(r => r.ProviderId == providerId.ToLower() &&
                        r.CreatedAt >= startOfMonth &&
                        r.CreatedAt < endOfMonth)
            .SumAsync(r => r.CharactersUsed);
    }

    public async Task<int> GetRemainingCharactersAsync(string providerId)
    {
        int currentUsage = await GetCurrentUsageAsync(providerId);
        int characterLimit = await GetCharacterLimitAsync(providerId);

        return Math.Max(0, characterLimit - currentUsage);
    }

    public async Task<bool> HasTemporaryOverrideAsync()
    {
        string? overrideValue = await _dbContext.Configurations
            .Where(c => c.Key == "tts_temporary_override_active")
            .Select(c => c.Value)
            .FirstOrDefaultAsync();

        return bool.TryParse(overrideValue, out bool result) && result;
    }

    public async Task SetTemporaryOverrideAsync(bool enabled)
    {
        await _dbContext.Configurations.Upsert(new()
            {
                Key = "tts_temporary_override_active",
                Value = enabled.ToString().ToLower()
            })
            .On(c => c.Key)
            .WhenMatched((existing, incoming) => new()
            {
                Key = existing.Key,
                Value = incoming.Value
            })
            .RunAsync();
    }

    public async Task<DateTime> GetCurrentBillingPeriodStartAsync()
    {
        // Get billing cycle configuration
        string? startDayStr = await _dbContext.Configurations
            .Where(c => c.Key == "tts_billing_cycle_start_day")
            .Select(c => c.Value)
            .FirstOrDefaultAsync();

        int startDay = int.TryParse(startDayStr, out int day) ? day : 1;
        DateTime now = DateTime.UtcNow;

        // Calculate the start of current billing period
        DateTime periodStart = new(now.Year, now.Month, Math.Min(startDay, DateTime.DaysInMonth(now.Year, now.Month)));

        // If we're before the start day, the billing period started last month
        if (now.Day < startDay)
        {
            periodStart = periodStart.AddMonths(-1);
            periodStart = new(periodStart.Year, periodStart.Month,
                Math.Min(startDay, DateTime.DaysInMonth(periodStart.Year, periodStart.Month)));
        }

        return periodStart;
    }

    public async Task<DateTime> GetCurrentBillingPeriodEndAsync()
    {
        DateTime periodStart = await GetCurrentBillingPeriodStartAsync();

        // Get billing cycle length
        string? cycleLengthStr = await _dbContext.Configurations
            .Where(c => c.Key == "tts_billing_cycle_length_days")
            .Select(c => c.Value)
            .FirstOrDefaultAsync();

        int cycleLength = int.TryParse(cycleLengthStr, out int length) ? length : 30;

        return periodStart.AddDays(cycleLength);
    }

    private async Task<int> GetCharacterLimitAsync(string providerId)
    {
        string configKey = providerId.ToLower() switch
        {
            "azure" => "tts_azure_character_limit",
            _ => "tts_character_limit_default"
        };

        string? limitStr = await _dbContext.Configurations
            .Where(c => c.Key == configKey)
            .Select(c => c.Value)
            .FirstOrDefaultAsync();

        // FIXED: Use 50 as default instead of 500,000
        return int.TryParse(limitStr, out int limit) ? limit : 50;
    }

    /// <summary>
    /// Checks if the provider has exceeded its monthly character limit
    /// </summary>
    public async Task<bool> IsMonthlyLimitExceededAsync(string providerId, int additionalCharacters = 0)
    {
        // Get provider settings from database
        TtsProvider? provider = await _dbContext.TtsProviders
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == providerId);

        if (provider == null || provider.MonthlyCharacterLimit <= 0) return false; // No limit configured

        // Calculate current month's usage
        DateTime startOfMonth = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        DateTime endOfMonth = startOfMonth.AddMonths(1);

        int currentMonthUsage = await _dbContext.TtsUsageRecords
            .AsNoTracking()
            .Where(r => r.ProviderId == providerId &&
                        r.CreatedAt >= startOfMonth &&
                        r.CreatedAt < endOfMonth)
            .SumAsync(r => r.CharactersUsed);

        // Check if current usage + new request would exceed limit
        return currentMonthUsage + additionalCharacters > provider.MonthlyCharacterLimit;
    }

    /// <summary>
    /// Gets current month's usage statistics for a provider
    /// </summary>
    public async Task<(int charactersUsed, decimal totalCost, int remainingCharacters)> GetCurrentMonthUsageAsync(
        string providerId)
    {
        DateTime startOfMonth = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        DateTime endOfMonth = startOfMonth.AddMonths(1);

        var usageStats = await _dbContext.TtsUsageRecords
            .AsNoTracking()
            .Where(r => r.ProviderId == providerId &&
                        r.CreatedAt >= startOfMonth &&
                        r.CreatedAt < endOfMonth)
            .GroupBy(r => 1)
            .Select(g => new
            {
                CharactersUsed = g.Sum(r => r.CharactersUsed),
                TotalCost = g.Sum(r => r.Cost)
            })
            .FirstOrDefaultAsync();

        int charactersUsed = usageStats?.CharactersUsed ?? 0;
        decimal totalCost = usageStats?.TotalCost ?? 0;
        int remainingCharacters = await GetRemainingCharactersAsync(providerId);

        return (charactersUsed, totalCost, remainingCharacters);
    }

    /// <summary>
    /// Debug method to show current character limit configuration
    /// </summary>
    public async Task<string> GetCharacterLimitDebugInfoAsync(string providerId)
    {
        string configKey = providerId.ToLower() switch
        {
            "azure" => "tts_azure_character_limit",
            _ => "tts_character_limit_default"
        };

        string? limitStr = await _dbContext.Configurations
            .Where(c => c.Key == configKey)
            .Select(c => c.Value)
            .FirstOrDefaultAsync();

        int currentLimit = int.TryParse(limitStr, out int limit) ? limit : 0;
        int currentUsage = await GetCurrentUsageAsync(providerId);
        int remaining = await GetRemainingCharactersAsync(providerId);

        return $"Provider: {providerId}\n" +
               $"Config Key: {configKey}\n" +
               $"Database Value: {limitStr ?? "NULL"}\n" +
               $"Parsed Limit: {currentLimit}\n" +
               $"Current Usage: {currentUsage}\n" +
               $"Remaining: {remaining}";
    }

    /// <summary>
    /// Set the character limit for a provider
    /// </summary>
    public async Task SetCharacterLimitAsync(string providerId, int characterLimit)
    {
        string configKey = providerId.ToLower() switch
        {
            "azure" => "tts_azure_character_limit",
            _ => "tts_character_limit_default"
        };

        await _dbContext.Configurations.Upsert(new()
            {
                Key = configKey,
                Value = characterLimit.ToString()
            })
            .On(c => c.Key)
            .WhenMatched((existing, incoming) => new()
            {
                Key = existing.Key,
                Value = incoming.Value
            })
            .RunAsync();
    }
}
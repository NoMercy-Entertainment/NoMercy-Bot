using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Globals.SystemCalls;
using NoMercyBot.Services.Twitch;
using NoMercyBot.Services.Twitch.Dto;

namespace NoMercyBot.Services.Seeds;

public static class RewardSeed
{
    public static async Task Init(this AppDbContext dbContext, IServiceScope scope)
    {
        try
        {
            TwitchApiService twitchApiService =
                scope.ServiceProvider.GetRequiredService<TwitchApiService>();

            // TwitchConfig may not be initialized yet during seeding — load directly from DB
            string broadcasterId = TwitchConfig.Service().UserId;
            if (string.IsNullOrEmpty(broadcasterId))
            {
                Service? twitchService = await dbContext
                    .Services.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Name == "Twitch");
                if (twitchService != null)
                {
                    TwitchConfig._service = twitchService;
                    broadcasterId = twitchService.UserId ?? string.Empty;
                }
            }

            if (string.IsNullOrEmpty(broadcasterId))
            {
                Logger.Setup(
                    "No broadcaster ID found in Twitch configuration - skipping reward seeding"
                );
                return;
            }

            // Fetch custom rewards from Twitch API
            ChannelPointsCustomRewardsResponse? rewardsResponse =
                await twitchApiService.GetCustomRewards(broadcasterId);

            if (rewardsResponse?.Data == null || !rewardsResponse.Data.Any())
            {
                Logger.Setup("No custom rewards found on Twitch channel - skipping reward seeding");
                return;
            }

            int added = 0;
            foreach (ChannelPointsCustomRewardsResponseData twitchReward in rewardsResponse.Data)
            {
                bool exists = await dbContext.Rewards.AnyAsync(r => r.Id == twitchReward.Id);
                if (exists)
                    continue;

                dbContext.Rewards.Add(
                    new()
                    {
                        Id = twitchReward.Id,
                        Title = twitchReward.Title,
                        Response = $"Thank you for redeeming {twitchReward.Title}!",
                        Permission = "everyone",
                        IsEnabled = twitchReward.IsEnabled,
                        Description = twitchReward.Prompt,
                    }
                );
                added++;
            }

            if (added > 0)
                await dbContext.SaveChangesAsync();

            Logger.Setup(
                $"Reward seed: {added} new, {rewardsResponse.Data.Count - added} already existed"
            );
        }
        catch (Exception ex)
        {
            Logger.Setup($"failed to fetch custom rewards from Twitch API during seeding: {ex}");
        }
    }
}

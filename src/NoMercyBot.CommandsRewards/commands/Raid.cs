using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Database.Models.ChatMessage;
using NoMercyBot.Globals.SystemCalls;
using NoMercyBot.Services.Twitch;
using NoMercyBot.Services.Twitch.Scripting;
using NoMercyBot.Services.Interfaces;
using NoMercyBot.Services.Twitch.Dto;
using NoMercyBot.Services.Obs;
using NoMercyBot.Services.Spotify;
using Serilog.Events;
using System.Threading;

public class RaidCommand: IBotCommand
{
    public string Name => "raid";
    public CommandPermission Permission => CommandPermission.Broadcaster;

    public async Task Init(CommandScriptContext ctx)
    {
    }

    public async Task Callback(CommandScriptContext ctx)
    {
        // No args → show ranked raid candidates instead of erroring out.
        if (ctx.Arguments.Length == 0)
        {
            await ShowSuggestions(ctx);
            return;
        }

        string targetUsername = ctx.Arguments[0].Replace("@", "").ToLower();
        int raidDelaySeconds = 60; // Default 60 seconds

        // Parse optional delay argument. Capped at 60s — Twitch's raid window is
        // only 90s, and we need buffer for OBS to actually stop the stream.
        if (ctx.Arguments.Length > 1 && int.TryParse(ctx.Arguments[1], out int customDelay))
        {
            raidDelaySeconds = Math.Clamp(customDelay, 10, 60);
        }

        try
        {
            User targetUser = await ctx.TwitchApiService.GetOrFetchUser(name: targetUsername);
            ChannelInfo targetChannelInfo = await ctx.TwitchApiService.GetOrFetchChannelInfo(id: targetUser.Id);

            StreamInfo? streamInfo = await ctx.TwitchApiService.GetStreamInfo(broadcasterId: targetUser.Id);
            if (streamInfo == null)
            {
                await ctx.TwitchChatService.SendReplyAsBot(
                    ctx.Message.Broadcaster.Username,
                    $"@{ctx.Message.User.DisplayName} {targetUser.DisplayName} is not currently live. You can only raid live channels!",
                    ctx.Message.Id);
                return;
            }

            // Fire the Twitch raid IMMEDIATELY — its 90s commit window starts
            // ticking the moment this returns, so the earlier the better. OBS
            // scene switch is independent, run it in parallel.
            Task initRaid = InitializeRaid(ctx, targetUser);
            Task sceneSwitch = SwitchToEndingScene(ctx);
            await Task.WhenAll(initRaid, sceneSwitch);

            await AnnounceRaid(ctx, targetUser, raidDelaySeconds);
            await RunCountdown(ctx, Math.Max(raidDelaySeconds - 3, 1));
            await CommitRaid(ctx, targetUser);
            await PauseSpotify(ctx);
        }
        catch (Exception ex)
        {
            Logger.Twitch($"Error in raid command: {ex.Message}", LogEventLevel.Error);
            await ctx.TwitchChatService.SendReplyAsBot(
                ctx.Message.Broadcaster.Username,
                $"@{ctx.Message.User.DisplayName} An error occurred while setting up the raid. Please try again or raid manually!",
                ctx.Message.Id);
        }
    }

    private async Task ShowSuggestions(CommandScriptContext ctx)
    {
        try
        {
            RaidSuggestionService service =
                ctx.ServiceProvider.GetRequiredService<RaidSuggestionService>();

            List<RaidCandidate> candidates = await service.GetSuggestionsAsync(max: 10);

            if (candidates.Count == 0)
            {
                await ctx.TwitchChatService.SendReplyAsBot(
                    ctx.Message.Broadcaster.Username,
                    "No raid candidates right now — either no one's live or the API isn't cooperating.",
                    ctx.Message.Id);
                return;
            }

            StringBuilder sb = new();
            sb.Append("Raid candidates: ");
            for (int i = 0; i < candidates.Count; i++)
            {
                if (i > 0) sb.Append(" | ");
                RaidCandidate c = candidates[i];
                string source = c.IsFollowed ? "F" : "N";
                string category = AbbreviateCategory(c.GameName);
                string lastRaid = FormatLastRaid(c.LastRaidedAt);
                sb.Append($"{i + 1}) {c.UserName} ({source}, {category}, {c.ViewerCount}v, {lastRaid})");
            }

            await ctx.TwitchChatService.SendReplyAsBot(
                ctx.Message.Broadcaster.Username,
                sb.ToString(),
                ctx.Message.Id);
        }
        catch (Exception ex)
        {
            Logger.Twitch($"Error fetching raid suggestions: {ex.Message}", LogEventLevel.Error);
            await ctx.TwitchChatService.SendReplyAsBot(
                ctx.Message.Broadcaster.Username,
                $"Failed to fetch raid suggestions: {ex.Message}",
                ctx.Message.Id);
        }
    }

    private static string FormatLastRaid(DateTime? lastRaidedAt)
    {
        if (lastRaidedAt is null) return "never";
        TimeSpan ago = DateTime.UtcNow - lastRaidedAt.Value;
        if (ago.TotalDays >= 30) return $"{(int)(ago.TotalDays / 30)}mo ago";
        if (ago.TotalDays >= 7) return $"{(int)(ago.TotalDays / 7)}w ago";
        if (ago.TotalDays >= 1) return $"{(int)ago.TotalDays}d ago";
        if (ago.TotalHours >= 1) return $"{(int)ago.TotalHours}h ago";
        return "today";
    }

    private static string AbbreviateCategory(string gameName)
    {
        if (string.IsNullOrEmpty(gameName)) return "?";
        return gameName switch
        {
            "Software and Game Development" => "Software",
            "Science & Technology" => "Sci&Tech",
            "Just Chatting" => "Chat",
            "Music" => "Music",
            "Talk Shows & Podcasts" => "Talk",
            "Art" => "Art",
            "Special Events" => "Event",
            // Default: truncate long category names
            _ => gameName.Length > 14 ? gameName.Substring(0, 12) + "…" : gameName,
        };
    }

    private async Task SwitchToEndingScene(CommandScriptContext ctx)
    {
        try
        {
            ObsApiService obsService = (ObsApiService)ctx.ServiceProvider.GetService(typeof(ObsApiService));
            if (obsService != null)
            {
                await obsService.SetCurrentScene("Ending");
                Logger.Twitch("Switched OBS scene to 'Ending'", LogEventLevel.Information);
            }
        }
        catch (Exception ex)
        {
            Logger.Twitch($"Could not switch OBS scene: {ex.Message}", LogEventLevel.Warning);
        }
    }

    private async Task InitializeRaid(CommandScriptContext ctx, User targetUser)
    {
        await ctx.TwitchApiService.RaidAsync(ctx.BroadcasterId, targetUser.Id);
        Logger.Twitch($"Raid initialized to {targetUser.DisplayName}", LogEventLevel.Information);
    }

    private async Task AnnounceRaid(CommandScriptContext ctx, User targetUser, int raidDelaySeconds)
    {
        await ctx.TwitchChatService.SendMessageAsBot(
            ctx.Message.Broadcaster.Username,
            $"RAID INCOMING to {targetUser.DisplayName}! Raiding in {raidDelaySeconds} seconds...");

        await Task.Delay(1000);

        await ctx.TwitchChatService.SendMessageAsBot(
            ctx.Message.Broadcaster.Username,
            "Big bird raid stoney90Hmmm Big bird raid stoney90Hmmm Big bird raid stoney90Hmmm Big bird raid stoney90Hmmm");

        await Task.Delay(500);

        await ctx.TwitchChatService.SendMessageAsBot(
            ctx.Message.Broadcaster.Username,
            "Big bird raid 🦅 Big bird raid 🦅 Big bird raid 🦅 Big bird raid 🦅 Big bird raid 🦅");
    }

    private async Task RunCountdown(CommandScriptContext ctx, int raidDelaySeconds)
    {
        int[] countdownTimes = { 45, 30, 15, 10, 5, 3, 2, 1 };
        foreach (int timeLeft in countdownTimes)
        {
            if (timeLeft >= raidDelaySeconds) continue;

            int waitTime = raidDelaySeconds - timeLeft;
            if (waitTime > 0)
            {
                await Task.Delay(waitTime * 1000, ctx.CancellationToken);
                raidDelaySeconds = timeLeft;
            }

            if (timeLeft <= 15)
            {
                await ctx.TwitchChatService.SendMessageAsBot(
                    ctx.Message.Broadcaster.Username,
                    $"Raid in {timeLeft} second{(timeLeft != 1 ? "s" : "")}...");
            }
        }

        if (raidDelaySeconds > 0)
        {
            await Task.Delay(raidDelaySeconds * 1000, ctx.CancellationToken);
        }
    }

    private async Task CommitRaid(CommandScriptContext ctx, User targetUser)
    {
        await ctx.TwitchChatService.SendMessageAsBot(
            ctx.Message.Broadcaster.Username,
            $"RAID LIVE! We're heading to {targetUser.DisplayName}! Let's go!");

        try
        {
            ObsApiService obsService = (ObsApiService)ctx.ServiceProvider.GetService(typeof(ObsApiService));
            if (obsService != null)
            {
                await obsService.StopStreaming();
                Logger.Twitch("Stopped OBS stream - raid committed", LogEventLevel.Information);
            }
        }
        catch (Exception ex)
        {
            Logger.Twitch($"Could not stop OBS stream: {ex.Message}", LogEventLevel.Warning);
        }
    }

    private async Task PauseSpotify(CommandScriptContext ctx)
    {
        try
        {
            SpotifyApiService spotifyService = (SpotifyApiService)ctx.ServiceProvider.GetService(typeof(SpotifyApiService));
            if (spotifyService != null)
            {
                await spotifyService.Pause();
                Logger.Twitch("Paused Spotify playback", LogEventLevel.Information);
            }
        }
        catch (Exception ex)
        {
            Logger.Twitch($"Could not pause Spotify: {ex.Message}", LogEventLevel.Warning);
        }
    }
}

return new RaidCommand();

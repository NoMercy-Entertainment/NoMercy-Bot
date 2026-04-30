using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NoMercyBot.Database.Models;
using NoMercyBot.Services.Interfaces;
using NoMercyBot.Services.Spotify;
using NoMercyBot.Services.Spotify.Dto;
using NoMercyBot.Services.Twitch.Scripting;
using SpotifyAPI.Web;

public class WrongSongRecord
{
    public string SongId { get; set; } = null!;
}

public class WrongSongCommand : IBotCommand
{
    public string Name => "wrongsong";
    public CommandPermission Permission => CommandPermission.Everyone;

    private const string STORAGE_KEY = "Spotify";

    public Task Init(CommandScriptContext ctx) => Task.CompletedTask;

    public async Task Callback(CommandScriptContext ctx)
    {
        string userId = ctx.Message.UserId;
        string displayName = ctx.Message.DisplayName;
        string broadcasterLogin = ctx.Message.Broadcaster.Username;

        try
        {
            Record? lastRecord = await ctx.DatabaseContext.Records
                .Where(r => r.UserId == userId && r.RecordType == STORAGE_KEY)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(ctx.CancellationToken);

            if (lastRecord == null)
            {
                await ctx.TwitchChatService.SendReplyAsBot(broadcasterLogin,
                    $"You haven't requested any songs to retract.", ctx.Message.Id);
                return;
            }

            WrongSongRecord? data = JsonConvert.DeserializeObject<WrongSongRecord>(lastRecord.Data);
            string? trackId = data?.SongId;
            if (string.IsNullOrEmpty(trackId))
            {
                ctx.DatabaseContext.Records.Remove(lastRecord);
                await ctx.DatabaseContext.SaveChangesAsync(ctx.CancellationToken);
                await ctx.TwitchChatService.SendReplyAsBot(broadcasterLogin,
                    $"Your last request had no track ID. Cleared.", ctx.Message.Id);
                return;
            }

            SpotifyApiService spotify = ctx.ServiceProvider.GetRequiredService<SpotifyApiService>();
            FullTrack? track = await spotify.GetTrack(trackId);
            string trackLabel = track != null
                ? $"{track.Name} by {track.Artists?.FirstOrDefault()?.Name ?? "?"}"
                : "your last request";

            string? currentTrackId = spotify.SpotifyState?.Item?.Id;
            bool isPlayingNow = !string.IsNullOrEmpty(currentTrackId) && currentTrackId == trackId;

            bool isQueued = false;
            if (!isPlayingNow)
            {
                SpotifyQueueResponse? queue = await spotify.GetQueue();
                isQueued = queue?.Queue?.Any(q => q.Id == trackId) == true;
            }

            if (!isPlayingNow && !isQueued)
            {
                ctx.DatabaseContext.Records.Remove(lastRecord);
                await ctx.DatabaseContext.SaveChangesAsync(ctx.CancellationToken);
                await ctx.TwitchChatService.SendReplyAsBot(broadcasterLogin,
                    $"Too late — {trackLabel} already played.", ctx.Message.Id);
                return;
            }

            if (isPlayingNow)
            {
                await spotify.NextTrack();
                await ctx.TwitchChatService.SendReplyAsBot(broadcasterLogin,
                    $"Skipped {trackLabel}.", ctx.Message.Id);
            }
            else
            {
                spotify.MarkForSkip(trackId);
                await ctx.TwitchChatService.SendReplyAsBot(broadcasterLogin,
                    $"Will auto-skip {trackLabel} when it plays.", ctx.Message.Id);
            }

            ctx.DatabaseContext.Records.Remove(lastRecord);
            await ctx.DatabaseContext.SaveChangesAsync(ctx.CancellationToken);
        }
        catch (Exception ex)
        {
            await ctx.TwitchChatService.SendReplyAsBot(broadcasterLogin,
                $"Failed to retract your last song: {ex.Message}", ctx.Message.Id);
        }
    }
}

return new WrongSongCommand();
